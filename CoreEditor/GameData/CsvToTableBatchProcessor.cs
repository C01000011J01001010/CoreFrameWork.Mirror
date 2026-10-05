using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;
using CoreEditor.Helpers;
using CoreEngine.GameData.Test;

namespace CoreEditor.GameData
{
    public class CsvToTableBatchProcessor : EditorWindow
    {
        #region 데이터 구조체 및 UI 상태
        private class TableProcessInfo
        {
            public bool IsSelected = true;
            public _Table TableAsset;
            public TextAsset MatchedCsv;
            public string StatusMessage = "대기 중";
            public MessageType StatusType = MessageType.None;
            public List<TextAsset> ConflictedCsvs = new List<TextAsset>();
        }

        private class FieldSchema
        {
            public int ColumnIndex;
            public string FieldName;
            public FieldInfo Field;
            public Type FieldType;
        }

        private List<TableProcessInfo> _tableInfos = new List<TableProcessInfo>();
        private Vector2 _scrollPosition;
        private string _searchQuery = "";
        #endregion

        private string tableTypeName = nameof(_Table);

        public const string WindowName = "CSV To Table Batch Processor";
        [MenuItem(Constants.ToolRootGameData + WindowName, priority = Constants.GameDataPriority + 2)]
        private static void ShowWindow()
        {
            var window = GetWindow<CsvToTableBatchProcessor>(WindowName);
            window.minSize = GameDataNavigationHelper.TapSize;
            window.Show();
        }

        private void OnEnable() => ScanProjectForTables();

        #region 에디터 윈도우 UI 렌더링
        private void OnGUI()
        {
            GameDataNavigationHelper.DrawTopNavigationBar(GameDataNavigationHelper.Tab.CsvProcessor);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🚀 일괄 데이터 변환 대시보드", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawSettings();
            EditorGUILayout.Space();

            _searchQuery = EditorGUILayout.TextField("🔍 검색 (테이블명)", _searchQuery, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space();

            DrawTableList();

            GUILayout.FlexibleSpace();
            EditorGUILayout.Space();
            DrawBottomActions();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180f;

            CsvToTableBatchProcessorSettings.Instance.IgnorePrefix = EditorGUILayout.TextField(
                new GUIContent("무시할 접두사 (Ignore Prefix)"),
                CsvToTableBatchProcessorSettings.Instance.IgnorePrefix);

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(CsvToTableBatchProcessorSettings.Instance);
                ScanProjectForTables();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTableList()
        {
            EditorGUILayout.LabelField($"📊 데이터 테이블 목록 (총 {_tableInfos.Count}개)", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");

            if (_tableInfos.Count == 0)
                EditorGUILayout.HelpBox($"프로젝트 내에 {tableTypeName}을 상속받는 에셋이 없습니다.", MessageType.Info);

            var filteredInfos = _tableInfos.Where(info =>
                string.IsNullOrEmpty(_searchQuery) ||
                info.TableAsset.name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var info in filteredInfos)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();

                info.IsSelected = EditorGUILayout.Toggle(info.IsSelected, GUILayout.Width(20));

                GUIStyle boldStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
                EditorGUILayout.LabelField(info.TableAsset.name, boldStyle, GUILayout.Width(150));

                if (info.MatchedCsv != null)
                {
                    GUI.contentColor = Color.green;
                    EditorGUILayout.LabelField($"<- {info.MatchedCsv.name}.csv", GUILayout.Width(150));
                    GUI.contentColor = Color.white;

                    if (GUILayout.Button("🔍 파일 위치", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        Selection.activeObject = info.MatchedCsv;
                        EditorGUIUtility.PingObject(info.MatchedCsv);
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUI.contentColor = Color.red;
                    EditorGUILayout.LabelField($"<- [CSV 찾을 수 없음 또는 충돌]", GUILayout.ExpandWidth(true));
                    GUI.contentColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();

                if (info.ConflictedCsvs.Count > 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space(25, false);
                    EditorGUILayout.LabelField("🔍 충돌된 파일 위치 확인:", GUILayout.Width(140));
                    foreach (var csvAsset in info.ConflictedCsvs)
                    {
                        if (GUILayout.Button(csvAsset.name, EditorStyles.miniButton, GUILayout.Width(100)))
                        {
                            Selection.activeObject = csvAsset;
                            EditorGUIUtility.PingObject(csvAsset);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (info.StatusType != MessageType.None)
                {
                    EditorGUILayout.HelpBox(info.StatusMessage, info.StatusType);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawBottomActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 전체 다시 스캔", GUILayout.Height(40))) ScanProjectForTables();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("✨ 체크된 항목 일괄 변환", GUILayout.Height(40))) ExecuteBatchConversion();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region 스캐닝 및 배치 처리 로직
        private void ScanProjectForTables()
        {
            _tableInfos.Clear();
            string[] tableGuids = AssetDatabase.FindAssets($"t:{tableTypeName}");

            foreach (string guid in tableGuids)
            {
                string tablePath = AssetDatabase.GUIDToAssetPath(guid);
                if (IsIgnoredPath(tablePath)) continue;

                _Table tableAsset = AssetDatabase.LoadAssetAtPath<_Table>(tablePath);
                if (tableAsset == null) continue;

                var info = new TableProcessInfo { TableAsset = tableAsset };
                string tableName = tableAsset.name;
                string[] csvGuids = AssetDatabase.FindAssets($"{tableName} t:{nameof(TextAsset)}");

                List<TextAsset> validCsvs = new List<TextAsset>();
                foreach (string cGuid in csvGuids)
                {
                    string csvPath = AssetDatabase.GUIDToAssetPath(cGuid);
                    if (IsIgnoredPath(csvPath)) continue;

                    if (Path.GetFileNameWithoutExtension(csvPath).Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
                        csvPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        validCsvs.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath));
                    }
                }

                if (validCsvs.Count == 1)
                {
                    info.MatchedCsv = validCsvs[0];
                    info.StatusMessage = "변환 대기 중";
                    info.StatusType = MessageType.Info;
                }
                else if (validCsvs.Count > 1)
                {
                    info.IsSelected = false;
                    info.ConflictedCsvs = validCsvs;
                    info.StatusMessage = $"이름이 동일한 CSV가 여러 개({validCsvs.Count}개) 발견되었습니다. 위 버튼을 눌러 위치를 확인하세요.";
                    info.StatusType = MessageType.Error;
                }
                else
                {
                    info.IsSelected = false;
                    info.StatusMessage = "이름이 일치하는 CSV 파일을 찾을 수 없습니다.";
                    info.StatusType = MessageType.Error;
                }

                _tableInfos.Add(info);
            }
        }

        private bool IsIgnoredPath(string path)
        {
            string ignorePrefix = CsvToTableBatchProcessorSettings.Instance.IgnorePrefix;
            if (string.IsNullOrWhiteSpace(ignorePrefix)) return false;

            foreach (string part in path.Split('/'))
            {
                if (part.StartsWith(ignorePrefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void ExecuteBatchConversion()
        {
            int successCount = 0, failCount = 0;

            foreach (var info in _tableInfos)
            {
                if (!info.IsSelected || info.MatchedCsv == null) continue;

                try
                {
                    ITableSetter tableSetter = info.TableAsset as ITableSetter;
                    if (tableSetter == null) throw new InvalidOperationException("IDataTableSetter 인터페이스 미구현");

                    Type recordType = GetRecordType(info.TableAsset.GetType());
                    MethodInfo convertMethod = typeof(CsvToTableBatchProcessor).GetMethod(nameof(ConvertCsvGeneric), BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo genericMethod = convertMethod.MakeGenericMethod(recordType);

                    genericMethod.Invoke(this, new object[] { tableSetter, info.MatchedCsv.text });

                    info.StatusMessage = "✅ 변환 성공";
                    info.StatusType = MessageType.Info;
                    successCount++;
                }
                catch (TargetInvocationException ex)
                {
                    info.StatusMessage = $"[변환 실패] {ex.InnerException?.Message}";
                    info.StatusType = MessageType.Error;
                    failCount++;
                }
                catch (Exception ex)
                {
                    info.StatusMessage = $"[시스템 에러] {ex.Message}";
                    info.StatusType = MessageType.Error;
                    failCount++;
                }
            }

            AssetDatabase.SaveAssets();
            this.ShowNotification(new GUIContent($"일괄 변환 완료! (성공: {successCount}, 실패: {failCount})"));
        }
        #endregion

        #region 코어 파이프라인 (리팩터링 구역)

        /// <summary>
        /// 5단계 모듈화된 메인 컨버팅 파이프라인 (오케스트레이터)
        /// </summary>
        private void ConvertCsvGeneric<TRecord>(ITableSetter tableSetter, string csvText)
            where TRecord : BaseRecord, IRecord, new()
        {
            string[] lines = SplitLines(csvText);

            // 1. CSV 라인 분할 및 스키마(헤더) 위치 탐색
            var headers = ExtractSchemaHeaders(lines, out int schemaRowIndex);

            // 2. CSV 헤더와 C# 필드 간의 리플렉션 매핑 및 유효성 검사
            ValidateAndMapFields<TRecord>(headers);

            // 3. 실제 데이터 파싱 및 객체 생성
            var records = ParseRecords<TRecord>(lines, schemaRowIndex + 1, headers);

            // 4. 생성된 객체들로부터 _AssetId -> PreloadCommand 일괄 추출 (베이킹 준비)
            var bakedCommands = ExtractPreloadCommands<TRecord>(records);

            // 5. 인터페이스를 통한 최종 데이터 주입 및 에셋 저장
            ApplyToTableAsset(tableSetter, records, bakedCommands);
        }

        private List<FieldSchema> ExtractSchemaHeaders(string[] lines, out int schemaRowIndex)
        {
            schemaRowIndex = -1;
            string[] schemaColumns = null;

            for (int r = 0; r < lines.Length; r++)
            {
                if (string.IsNullOrWhiteSpace(lines[r])) continue;
                string[] cols = CsvParserHelper.ParseLine(lines[r]);

                int firstCol = FindFirstSchemaColumn(cols);
                if (firstCol >= 0)
                {
                    schemaRowIndex = r;
                    schemaColumns = cols;
                    break;
                }
            }

            if (schemaRowIndex < 0)
                throw new Exception("CSV에서 중괄호 { } 로 감싸진 헤더 영역을 찾지 못했습니다.");

            var csvHeaders = new List<FieldSchema>();
            for (int col = 0; col < schemaColumns.Length; col++)
            {
                string value = schemaColumns[col].Trim();
                if (IsSchemaField(value))
                {
                    csvHeaders.Add(new FieldSchema { ColumnIndex = col, FieldName = GetFieldName(value) });
                }
            }
            return csvHeaders;
        }

        private void ValidateAndMapFields<TRecord>(List<FieldSchema> csvHeaders)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            List<FieldInfo> csFields = new List<FieldInfo>();
            Type currentType = typeof(TRecord);

            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType.GetFields(flags).Where(f => f.GetCustomAttribute<TableColumnAttribute>() != null);
                csFields.AddRange(fields);
                currentType = currentType.BaseType;
            }

            var csOnly = csFields.Where(f => !csvHeaders.Any(h => string.Equals(h.FieldName, f.Name, StringComparison.OrdinalIgnoreCase))).Select(f => f.Name).ToList();
            var csvOnly = csvHeaders.Where(h => !csFields.Any(f => string.Equals(f.Name, h.FieldName, StringComparison.OrdinalIgnoreCase))).Select(h => h.FieldName).ToList();

            if (csOnly.Count > 0 || csvOnly.Count > 0)
            {
                List<string> errorLines = new List<string> { "컬럼 매칭 실패!" };
                if (csOnly.Count > 0) errorLines.Add($"CSV 누락 (C#에만 존재): {string.Join(", ", csOnly)}");
                if (csvOnly.Count > 0) errorLines.Add($"C# 누락 (CSV에만 존재): {string.Join(", ", csvOnly)}");
                throw new Exception(string.Join("\n", errorLines));
            }

            foreach (var header in csvHeaders)
            {
                FieldInfo matchedField = csFields.First(f => string.Equals(f.Name, header.FieldName, StringComparison.OrdinalIgnoreCase));
                header.Field = matchedField;
                header.FieldType = matchedField.FieldType;
            }
        }

        private List<IRecord> ParseRecords<TRecord>(string[] lines, int startRow, List<FieldSchema> csvHeaders)
            where TRecord : BaseRecord, IRecord, new()
        {
            List<IRecord> temporaryRecords = new List<IRecord>();

            // HashSet 대신 Dictionary를 사용하여 해시 충돌 원천 차단
            Dictionary<ulong, string> usedIdsMap = new Dictionary<ulong, string>();

            for (int row = startRow; row < lines.Length; row++)
            {
                string line = lines[row];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = CsvParserHelper.ParseLine(line);
                TRecord record = new TRecord();

                for (int i = 0; i < csvHeaders.Count; i++)
                {
                    var schema = csvHeaders[i];
                    string rawValue = schema.ColumnIndex < columns.Length ? columns[schema.ColumnIndex].Trim() : string.Empty;

                    try
                    {
                        object value = ConvertValue(rawValue, schema.FieldType, row, schema.FieldName);
                        schema.Field.SetValue(record, value);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"[행: {row + 1}, 열: '{schema.FieldName}'] 값('{rawValue}') 변환 실패: {ex.Message}");
                    }
                }

                // ID 베이킹 (문자열 -> XXH64 ulong 변환)
                ((IBakeId)record).BakeID();
                ulong recordId = record.ID;

                // BaseRecord에 숨겨진 _primarykey 원본 문자열을 리플렉션으로 빼오기 (에디터 전용이므로 성능 부담 없음)
                string rawKey = "Unknown";
                Type currentType = record.GetType();
                while (currentType != null)
                {
                    var pkField = currentType.GetField("_primarykey", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (pkField != null)
                    {
                        rawKey = pkField.GetValue(record) as string ?? "Unknown";
                        break;
                    }
                    currentType = currentType.BaseType;
                }

                // 64비트 해시 충돌 및 중복 기입 검사 로직
                if (usedIdsMap.TryGetValue(recordId, out string existingKey))
                {
                    if (existingKey != rawKey)
                    {
                        // ID는 같은데 기획자가 적은 원본 텍스트가 다름 -> XXH64 알고리즘 충돌! (기적의 확률)
                        throw new Exception(
                            $"[CoreEngine FATAL] 해시 충돌(Hash Collision) 검출! (행: {row + 1})\n" +
                            $"Key A: '{existingKey}'\n" +
                            $"Key B: '{rawKey}'\n" +
                            $"두 문자열이 우연히 동일한 고유 ID({recordId})를 생성했습니다. Key B의 문자열을 살짝 변경해주세요.");
                    }
                    else
                    {
                        // 원본 텍스트도 완전히 같음 -> 기획자가 엑셀에 똑같은 키를 실수로 두 번 복붙함
                        throw new Exception($"[행: {row + 1}] 중복된 데이터(Key: '{rawKey}')가 엑셀에 존재합니다.");
                    }
                }

                usedIdsMap.Add(recordId, rawKey);
                temporaryRecords.Add(record);
            }

            return temporaryRecords;
        }

        struct Field2Type
        {
            public readonly FieldInfo fieldInfo;
            public readonly Type cmdType;
            public Field2Type(FieldInfo fieldInfo, Type cmdType)
            {
                this.fieldInfo = fieldInfo;
                this.cmdType = cmdType;
            }
        }
        private _AssetPreloadCommand[] ExtractPreloadCommands<TRecord>(List<IRecord> records)
        {
            // [최적화] FieldInfo와 매칭될 Command의 타입(Type)을 미리 캐싱
            var singleAssetIdFields = new List<Field2Type>();
            var arrayAssetIdFields = new List<Field2Type>();

            // AssetId<,> 타입인 필드들을 찾아 분류하고 Command 타입 미리 계산
            var allFieldInfos = typeof(TRecord).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fieldInfo in allFieldInfos)
            {
                Type ft = fieldInfo.FieldType;
                
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(AssetId<,>))
                {
                    Type[] genArgs = ft.GetGenericArguments();
                    Type cmdType = typeof(AssetPreloadCommand<,>).MakeGenericType(genArgs[0], genArgs[1]);
                    singleAssetIdFields.Add(new(fieldInfo, cmdType));
                }
                else if (ft.IsArray && ft.GetElementType().IsGenericType && ft.GetElementType().GetGenericTypeDefinition() == typeof(AssetId<,>))
                {
                    Type[] genArgs = ft.GetElementType().GetGenericArguments();
                    Type cmdType = typeof(AssetPreloadCommand<,>).MakeGenericType(genArgs[0], genArgs[1]);
                    arrayAssetIdFields.Add(new(fieldInfo, cmdType));
                }
            }

            // 타입별로 ID를 모아두는 딕셔너리 (중복 방지를 위해 HashSet 사용)
            Dictionary<Type, HashSet<ulong>> typeToIdsMap = new();

            foreach (IRecord record in records)
            {
                // 단일 필드 추출
                foreach (Field2Type field2Type in singleAssetIdFields)
                {
                    FieldInfo assetIdFieldInfo = field2Type.fieldInfo;
                    Type cmdType = field2Type.cmdType;

                    object assetIdObj = assetIdFieldInfo.GetValue(record);

                    // 리플렉션(GetProperty) 제거 -> 인터페이스를 통해 직접 호출 (압도적으로 빠름)
                    ulong id = ((IIdentifiable)assetIdObj).ID;

                    if (id > 0)
                    {
                        if (!typeToIdsMap.TryGetValue(cmdType, out var idSet))
                        {
                            idSet = new HashSet<ulong>();
                            typeToIdsMap[cmdType] = idSet;
                        }
                        idSet.Add(id); // HashSet이므로 알아서 중복 무시됨
                    }
                }

                // 배열 필드 추출
                foreach (Field2Type field2Type in arrayAssetIdFields)
                {
                    FieldInfo arrayFieldInfo = field2Type.fieldInfo;
                    Type cmdType = field2Type.cmdType;

                    if (arrayFieldInfo.GetValue(record) is Array arr)
                    {
                        if (!typeToIdsMap.TryGetValue(cmdType, out var idSet))
                        {
                            idSet = new HashSet<ulong>();
                            typeToIdsMap[cmdType] = idSet;
                        }

                        foreach (var item in arr)
                        {
                            // 배열도 인터페이스로 캐스팅하여 즉시 호출
                            ulong id = ((IIdentifiable)item).ID;
                            if (id > 0) idSet.Add(id);
                        }
                    }
                }
            }

            // 수집된 Map을 바탕으로 타입당 단 1개의 Command 객체만 생성하여 반환
            var bakedCommands = new List<_AssetPreloadCommand>();
            foreach (var kvp in typeToIdsMap)
            {
                Type cmdType = kvp.Key;
                ulong[] uniqueIdsArray = kvp.Value.ToArray(); // HashSet -> int[] 배열로 변환
                Array.Sort(uniqueIdsArray); // 보기 좋게 정렬

                var cmd = (_AssetPreloadCommand)Activator.CreateInstance(cmdType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new object[] { uniqueIdsArray }, null);

                bakedCommands.Add(cmd);
            }

            return bakedCommands.ToArray();
        }

        private void ApplyToTableAsset(ITableSetter tableSetter, List<IRecord> records, _AssetPreloadCommand[] bakedCommands)
        {
            tableSetter.Clear();
            tableSetter.Set(records);
            //tableSetter.SetCapacity(records.Count);
            //foreach (var rec in records)
            //{
            //    tableSetter.Add(rec);
            //}

            // 변경된 인터페이스 메서드 호출 (AssetId[] -> _AssetPreloadCommand[])
            tableSetter.BakePreloadCommands(bakedCommands);

            EditorUtility.SetDirty((UnityEngine.Object)tableSetter);
        }
        #endregion

        #region 타입 변환 및 헬퍼 함수 모음
        private static object ConvertValue(string value, Type targetType, int row, string columnName)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(targetType);

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                throw new Exception($"List<T> 타입은 사용할 수 없습니다. 데이터 압축을 위해 배열(T[])을 사용하세요.");

            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                string[] parts = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                Array array = Array.CreateInstance(elementType, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    array.SetValue(ConvertValue(parts[i].Trim(), elementType, row, columnName), i);
                }
                return array;
            }

            if (targetType == typeof(string)) return value;

            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null) targetType = nullableType;

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out object enumValue)) return enumValue;
                throw new FormatException($"'{value}'은(는) 유효한 열거형 값이 아닙니다.");
            }

            ConstructorInfo stringConstructor = targetType.GetConstructor(new Type[] { typeof(string) });
            if (stringConstructor != null)
            {
                return stringConstructor.Invoke(new object[] { value });
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsArray) return Array.CreateInstance(type.GetElementType(), 0);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static Type GetRecordType(Type tableType)
        {
            Type current = tableType;
            while (current != null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BaseTable<>))
                    return current.GetGenericArguments()[0];
                current = current.BaseType;
            }
            throw new InvalidOperationException("BaseDataTable 형식을 찾을 수 없습니다.");
        }

        private static int FindFirstSchemaColumn(string[] columns)
        {
            for (int column = 0; column < columns.Length; column++)
            {
                if (IsSchemaField(columns[column].Trim())) return column;
            }
            return -1;
        }

        private static bool IsSchemaField(string value) => value.Length >= 2 && value[0] == '{' && value[^1] == '}';
        private static string GetFieldName(string value) => value.Substring(1, value.Length - 2).Trim();
        private static string[] SplitLines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        #endregion
    }
}