using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;
using CoreEditor.Helpers;

namespace CoreEditor.GameData
{
    public class CsvToTableConverter : EditorWindow
    {
        private class TableProcessInfo
        {
            public bool IsSelected = true;
            public _DataTable TableAsset;
            public TextAsset MatchedCsv;
            public string StatusMessage = "대기 중";
            public MessageType StatusType = MessageType.None;

            public List<TextAsset> ConflictedCsvs = new List<TextAsset>();
        }

        private List<TableProcessInfo> _tableInfos = new List<TableProcessInfo>();
        private Vector2 _scrollPosition;

        [MenuItem(Constants.ToolRootGameData + "CSV Batch Converter Dashboard")]
        private static void Open()
        {
            var window = GetWindow<CsvToTableConverter>("Data Converter");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            ScanProjectForTables();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🚀 일괄 데이터 변환 대시보드", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawSettings();
            EditorGUILayout.Space();

            DrawBatchActionButtons();
            EditorGUILayout.Space();

            DrawTableList();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180f;

            CsvConverterSettings.Instance.IgnorePrefix = EditorGUILayout.TextField(
                new GUIContent("무시할 접두사 (Ignore Prefix)"),
                CsvConverterSettings.Instance.IgnorePrefix);

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(CsvConverterSettings.Instance);
                ScanProjectForTables();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBatchActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 전체 다시 스캔", GUILayout.Height(30)))
            {
                ScanProjectForTables();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("✨ 체크된 항목 일괄 변환", GUILayout.Height(30)))
            {
                ExecuteBatchConversion();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableList()
        {
            EditorGUILayout.LabelField($"📊 데이터 테이블 목록 (총 {_tableInfos.Count}개)", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");

            if (_tableInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("프로젝트 내에 _DataTable을 상속받는 에셋이 없습니다.", MessageType.Info);
            }

            foreach (var info in _tableInfos)
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

                    // [Fix] 1:1 매칭된 CSV 파일도 핑을 찍을 수 있는 버튼 추가
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

        // ==========================================================
        // 1. 스캔 및 자동 매칭 로직
        // ==========================================================
        private void ScanProjectForTables()
        {
            _tableInfos.Clear();

            string[] tableGuids = AssetDatabase.FindAssets("t:_DataTable");
            foreach (string guid in tableGuids)
            {
                string tablePath = AssetDatabase.GUIDToAssetPath(guid);
                if (IsIgnoredPath(tablePath)) continue;

                _DataTable tableAsset = AssetDatabase.LoadAssetAtPath<_DataTable>(tablePath);
                if (tableAsset == null) continue;

                var info = new TableProcessInfo { TableAsset = tableAsset };
                string tableName = tableAsset.name;
                string[] csvGuids = AssetDatabase.FindAssets($"{tableName} t:TextAsset");

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
            string ignorePrefix = CsvConverterSettings.Instance.IgnorePrefix;
            if (string.IsNullOrWhiteSpace(ignorePrefix)) return false;

            string[] parts = path.Split('/');
            foreach (string part in parts)
            {
                if (part.StartsWith(ignorePrefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // ==========================================================
        // 2. 일괄 변환 실행
        // ==========================================================
        private void ExecuteBatchConversion()
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var info in _tableInfos)
            {
                if (!info.IsSelected || info.MatchedCsv == null) continue;

                try
                {
                    IDataTableSetter tableSetter = info.TableAsset as IDataTableSetter;
                    if (tableSetter == null) throw new InvalidOperationException("IDataTableSetter 인터페이스 미구현");

                    Type recordType = GetRecordType(info.TableAsset.GetType());

                    MethodInfo convertMethod = typeof(CsvToTableConverter).GetMethod(nameof(ConvertCsvGeneric), BindingFlags.NonPublic | BindingFlags.Instance);
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
            EditorUtility.DisplayDialog("일괄 변환 결과", $"작업 완료!\n\n성공: {successCount}개\n실패: {failCount}개", "확인");
        }

        // ==========================================================
        // 3. 단일 테이블 파싱 및 검증 로직
        // ==========================================================
        private void ConvertCsvGeneric<TRecord>(IDataTableSetter tableSetter, string csvText)
            where TRecord : BaseDataRecord, IDataRecord, new()
        {
            string[] lines = SplitLines(csvText);
            int schemaRowIndex = -1;
            string[] schemaColumns = null;

            for (int r = 0; r < lines.Length; r++)
            {
                if (string.IsNullOrWhiteSpace(lines[r])) continue;
                string[] cols = CsvParserHelper.ParseLine(lines[r]);
                if (FindFirstSchemaColumn(cols) >= 0)
                {
                    schemaRowIndex = r;
                    schemaColumns = cols;
                    break;
                }
            }
            if (schemaRowIndex < 0) throw new Exception("CSV에서 중괄호 { } 로 감싸진 헤더 영역을 찾지 못했습니다.");

            List<FieldSchema> csvHeaders = new List<FieldSchema>();
            for (int col = 0; col < schemaColumns.Length; col++)
            {
                string value = schemaColumns[col].Trim();
                if (IsSchemaField(value))
                {
                    csvHeaders.Add(new FieldSchema { ColumnIndex = col, FieldName = GetFieldName(value) });
                }
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            List<FieldInfo> csFields = new List<FieldInfo>();
            Type currentType = typeof(TRecord);
            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType.GetFields(flags).Where(f => f.GetCustomAttribute<TableColumnAttribute>() != null);
                csFields.AddRange(fields);
                currentType = currentType.BaseType;
            }

            // [Fix] 교차 검증: C#에만 있는 필드와 CSV에만 있는 헤더를 각각 찾아서 보여줍니다.
            var csOnly = csFields
                .Where(f => !csvHeaders.Any(h => string.Equals(h.FieldName, f.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(f => f.Name)
                .ToList();

            var csvOnly = csvHeaders
                .Where(h => !csFields.Any(f => string.Equals(f.Name, h.FieldName, StringComparison.OrdinalIgnoreCase)))
                .Select(h => h.FieldName)
                .ToList();

            if (csOnly.Count > 0 || csvOnly.Count > 0)
            {
                List<string> errorLines = new List<string> { "컬럼 매칭 실패!" };
                if (csOnly.Count > 0) errorLines.Add($"CSV 누락 (C#에만 존재): {string.Join(", ", csOnly)}");
                if (csvOnly.Count > 0) errorLines.Add($"C# 누락 (CSV에만 존재): {string.Join(", ", csvOnly)}");

                throw new Exception(string.Join("\n", errorLines));
            }

            foreach (var header in csvHeaders)
            {
                // 위에서 교차 검증을 마쳤으므로 반드시 1:1 매칭됩니다.
                FieldInfo matchedField = csFields.First(f => string.Equals(f.Name, header.FieldName, StringComparison.OrdinalIgnoreCase));
                header.Field = matchedField;
                header.FieldType = matchedField.FieldType;
            }

            List<IDataRecord> temporaryRecords = new List<IDataRecord>();
            HashSet<int> usedIds = new HashSet<int>();

            for (int row = schemaRowIndex + 1; row < lines.Length; row++)
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

                if (!usedIds.Add(record.Id))
                {
                    throw new Exception($"[행: {row + 1}] 중복된 ID({record.Id})가 발견되었습니다.");
                }

                temporaryRecords.Add(record);
            }

            tableSetter.Clear();
            tableSetter.SetCapacity(temporaryRecords.Count);
            foreach (var rec in temporaryRecords)
            {
                tableSetter.Add(rec);
            }
            EditorUtility.SetDirty((UnityEngine.Object)tableSetter);
        }

        // ==========================================================
        // 4. 값 변환 및 배열 파싱 로직
        // ==========================================================
        private static object ConvertValue(string value, Type targetType, int row, string columnName)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(targetType);

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                throw new Exception($"List<T> 타입은 사용할 수 없습니다. 데이터 압축을 위해 배열(T[])을 사용하세요.");
            }

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
            if (nullableType != null)
            {
                targetType = nullableType;
            }

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out object enumValue)) return enumValue;
                throw new FormatException($"'{value}'은(는) 유효한 열거형 값이 아닙니다.");
            }

            ConstructorInfo intConstructor = targetType.GetConstructor(new Type[] { typeof(int) });
            if (intConstructor != null)
            {
                if (int.TryParse(value, out int parsedInt)) return intConstructor.Invoke(new object[] { parsedInt });
                throw new FormatException("AssetId 변환 실패 (정수가 아님)");
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
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BaseDataTable<>))
                {
                    return current.GetGenericArguments()[0];
                }
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

        private static bool IsSchemaField(string value)
        {
            return value.Length >= 2 && value[0] == '{' && value[^1] == '}';
        }

        private static string GetFieldName(string value)
        {
            return value.Substring(1, value.Length - 2).Trim();
        }

        private static string[] SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private class FieldSchema
        {
            public int ColumnIndex;
            public string FieldName;
            public FieldInfo Field;
            public Type FieldType;
        }
    }
}