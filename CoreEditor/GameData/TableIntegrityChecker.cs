using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;

namespace CoreEditor.GameData
{
    public class TableIntegrityChecker : EditorWindow
    {
        #region 데이터 구조체
        private class ReferenceError
        {
            public int RowIndex;          // 리스트 인덱스 (대략적인 CSV 행 번호)
            public string RecordKey;      // BaseRecord에 저장된 _primarykey (원본 문자열)
            public string FieldName;
            public string MissingValue;   // 💡 기획자가 잘못 입력한 원본 문자열 값
            public Type TargetType;
            public bool IsAssetId;        // true면 Registry 에러, false면 Table(CSV) 에러
        }

        private class TableErrorReport
        {
            public _Table TableAsset;
            public TextAsset MatchedCsv;
            public List<ReferenceError> Errors = new();
        }
        #endregion

        #region 상태 변수
        // 💡 툴이 열려있는 동안만 유지되는 O(1) 룩업 캐시
        private readonly Dictionary<Type, HashSet<ulong>> _validTableIds = new();
        private readonly Dictionary<Type, HashSet<ulong>> _validRegistryIds = new();

        private List<TableErrorReport> _errorReports = new();
        private Vector2 _scrollPosition;
        private bool _isScanning = false;
        #endregion

        public const string WindowName = "Table Integrity Checker";

        [MenuItem(Constants.ToolRootGameData + WindowName, priority = Constants.GameDataPriority + 3)]
        private static void Open()
        {
            var window = GetWindow<TableIntegrityChecker>(WindowName);
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable() => ScanAndCheck();

        #region 에디터 윈도우 UI 렌더링
        private void OnGUI()
        {
            DrawTopNavigationBar();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🔗 데이터 참조 무결성 검사기", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 전체 데이터 스캔 및 검사", GUILayout.Height(30)))
            {
                ScanAndCheck();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_isScanning)
            {
                EditorGUILayout.HelpBox("데이터를 스캔하는 중입니다...", MessageType.Info);
                return;
            }

            DrawErrorReports();
        }

        private void DrawTopNavigationBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("1. " + GameDataOrganizer.WindowName, EditorStyles.toolbarButton))
                GetWindow<GameDataOrganizer>(GameDataOrganizer.WindowName).Show();

            if (GUILayout.Button("2. " + PreloadAddressableSetter.WindowName, EditorStyles.toolbarButton))
                GetWindow<PreloadAddressableSetter>(PreloadAddressableSetter.WindowName).Show();

            if (GUILayout.Button("3. " + CsvToTableBatchProcessor.WindowName, EditorStyles.toolbarButton))
                GetWindow<CsvToTableBatchProcessor>(CsvToTableBatchProcessor.WindowName).Show();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("4. " + WindowName, EditorStyles.toolbarButton)) { }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawErrorReports()
        {
            if (_errorReports.Count == 0)
            {
                EditorGUILayout.HelpBox("끊어진 참조(Dead Link)가 하나도 없습니다. 완벽합니다!", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"🚨 발견된 에러 테이블: {_errorReports.Count}개", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");

            // 리치 텍스트를 허용하는 GUI 스타일 생성
            GUIStyle richTextStyle = new GUIStyle(EditorStyles.label) { richText = true };

            foreach (var report in _errorReports)
            {
                EditorGUILayout.BeginVertical("helpbox");

                // 테이블 헤더 영역
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📄 {report.TableAsset.name}", EditorStyles.boldLabel, GUILayout.Width(200));

                if (GUILayout.Button("🔍 테이블 에셋", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Selection.activeObject = report.TableAsset;
                    EditorGUIUtility.PingObject(report.TableAsset);
                }

                if (report.MatchedCsv != null)
                {
                    if (GUILayout.Button("📝 원본 CSV 열기", EditorStyles.miniButton, GUILayout.Width(120)))
                    {
                        Selection.activeObject = report.MatchedCsv;
                        EditorGUIUtility.PingObject(report.MatchedCsv);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                // 에러 상세 리스트 영역
                foreach (var error in report.Errors)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space(10, false); // 들여쓰기

                    // 💡 잘못된 값을 빨간색(<color=#FF4444>)으로 확실하게 강조하여 출력
                    string errorMsg = $"[행: {error.RowIndex}] <b>{error.FieldName}</b> 필드 ➔ 잘못된 값: <color=#FF4444><b>{error.MissingValue}</b></color>  <color=#888888>(PK: {error.RecordKey})</color>";
                    EditorGUILayout.LabelField(errorMsg, richTextStyle, GUILayout.ExpandWidth(true));

                    // 스마트 핑 버튼
                    GUI.contentColor = Color.yellow;
                    if (GUILayout.Button(error.IsAssetId ? "📂 Registry 확인" : "📄 CSV 확인", EditorStyles.miniButton, GUILayout.Width(120)))
                    {
                        PingTarget(error.TargetType, error.IsAssetId);
                    }
                    GUI.contentColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region 스캔 및 검사 핵심 로직
        private void ScanAndCheck()
        {
            _isScanning = true;
            _errorReports.Clear();

            // 1단계: O(1) 룩업을 위한 메모리 인덱스 빌드
            BuildValidationCaches();

            // 2단계: 모든 테이블을 순회하며 데드 링크 검사
            var tableGuids = AssetDatabase.FindAssets($"t:{nameof(_Table)}");
            foreach (var guid in tableGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                _Table tableAsset = AssetDatabase.LoadAssetAtPath<_Table>(path);
                if (tableAsset == null) continue;

                var report = CheckTable(tableAsset);
                if (report != null && report.Errors.Count > 0)
                {
                    // 테이블 이름으로 매칭되는 CSV 찾기 (스마트 핑용)
                    string[] csvGuids = AssetDatabase.FindAssets($"{tableAsset.name} t:TextAsset");
                    if (csvGuids.Length > 0)
                    {
                        report.MatchedCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(csvGuids[0]));
                    }
                    _errorReports.Add(report);
                }
            }

            _isScanning = false;
        }

        private void BuildValidationCaches()
        {
            _validTableIds.Clear();
            _validRegistryIds.Clear();

            // 모든 _Table의 ID 수집
            var tableGuids = AssetDatabase.FindAssets($"t:{nameof(_Table)}");
            foreach (var guid in tableGuids)
            {
                _Table table = AssetDatabase.LoadAssetAtPath<_Table>(AssetDatabase.GUIDToAssetPath(guid));
                if (table == null) continue;

                HashSet<ulong> ids = new();
                ExtractIdsFromScriptableObject(table, ids);
                _validTableIds[table.GetType()] = ids;
            }

            // 모든 _AssetRegistry의 ID 수집
            var registryGuids = AssetDatabase.FindAssets($"t:{nameof(_AssetRegistry)}");
            foreach (var guid in registryGuids)
            {
                var registry = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (registry == null) continue;

                var ids = new HashSet<ulong>();
                ExtractIdsFromScriptableObject(registry, ids);
                _validRegistryIds[registry.GetType()] = ids;
            }
        }

        private void ExtractIdsFromScriptableObject(ScriptableObject so, HashSet<ulong> idSet)
        {
            Type currentType = so.GetType();
            while (currentType != null && currentType != typeof(ScriptableObject))
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(string))
                    {
                        if (field.GetValue(so) is IEnumerable collection)
                        {
                            foreach (var item in collection)
                            {
                                if (item is IIdentifiable identifiableItem && identifiableItem.ID > 0)
                                {
                                    idSet.Add(identifiableItem.ID);
                                }
                            }
                        }
                    }
                }
                currentType = currentType.BaseType;
            }
        }

        private TableErrorReport CheckTable(_Table table)
        {
            var report = new TableErrorReport { TableAsset = table };
            bool hasRecords = false;

            Type currentType = table.GetType();
            while (currentType != null && currentType != typeof(ScriptableObject))
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(string))
                    {
                        if (field.GetValue(table) is IEnumerable collection)
                        {
                            int rowIndex = 0; // 💡 인덱스 추적 시작
                            foreach (var item in collection)
                            {
                                if (item is IRecord record)
                                {
                                    hasRecords = true;
                                    CheckRecordFields(record, rowIndex, report); // rowIndex 전달
                                    rowIndex++;
                                }
                            }
                        }
                    }
                }
                currentType = currentType.BaseType;
            }

            return hasRecords ? report : null;
        }

        private void CheckRecordFields(IRecord record, int rowIndex, TableErrorReport report)
        {
            // 💡 BaseRecord에 숨어있는 _primarykey 값 훔쳐오기
            string recordKey = "Unknown";
            Type recordType = record.GetType();
            while (recordType != null && recordType != typeof(object))
            {
                var pkField = recordType.GetField("_primarykey", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (pkField != null)
                {
                    recordKey = pkField.GetValue(record) as string ?? "Unknown";
                    break;
                }
                recordType = recordType.BaseType;
            }

            Type currentType = record.GetType();
            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    Type ft = field.FieldType;
                    bool isAssetId = false, isRecordId = false;

                    // 단일 타입 검사
                    if (ft.IsGenericType)
                    {
                        Type genDef = ft.GetGenericTypeDefinition();
                        isAssetId = genDef == typeof(AssetId<,>);
                        isRecordId = genDef == typeof(ForeignKey<,>);
                    }

                    if (isAssetId || isRecordId)
                    {
                        ValidateId(field.GetValue(record), ft, field.Name, rowIndex, recordKey, isAssetId, report);
                    }
                    // 배열 타입 검사
                    else if (ft.IsArray && ft.GetElementType().IsGenericType)
                    {
                        Type elemType = ft.GetElementType();
                        Type genDef = elemType.GetGenericTypeDefinition();
                        isAssetId = genDef == typeof(AssetId<,>);
                        isRecordId = genDef == typeof(ForeignKey<,>);

                        if (isAssetId || isRecordId)
                        {
                            if (field.GetValue(record) is Array arr)
                            {
                                foreach (var element in arr)
                                {
                                    ValidateId(element, elemType, field.Name, rowIndex, recordKey, isAssetId, report);
                                }
                            }
                        }
                    }
                }
                currentType = currentType.BaseType;
            }
        }

        private void ValidateId(object idObj, Type idType, string fieldName, int rowIndex, string recordKey, bool isAssetId, TableErrorReport report)
        {
            ulong id = ((IIdentifiable)idObj).ID;

            // ulong은 부호 없는 정수이므로 0보다 작을 수 없음
            if (id == 0) return;

            // 제네릭 2번째 인자가 항상 타겟 타입(TRegistry 또는 TTable)임
            Type targetType = idType.GetGenericArguments()[1];
            var cache = isAssetId ? _validRegistryIds : _validTableIds;

            // 캐시에 없거나(해당 타입 데이터가 아예 없음), 해당 ID가 없으면 에러
            if (!cache.TryGetValue(targetType, out var validIds) || !validIds.Contains(id))
            {
                string rawValue = (idObj as IReferenceKey)?.GetEditorRawKey() ?? "null";

                report.Errors.Add(new ReferenceError
                {
                    RowIndex = rowIndex,
                    RecordKey = recordKey,
                    FieldName = fieldName,
                    MissingValue = rawValue, // 💡 문제가 된 값 문자열화
                    TargetType = targetType,
                    IsAssetId = isAssetId
                });
            }
        }

        private void PingTarget(Type targetType, bool isAssetId)
        {
            string searchFilter = isAssetId ? $"t:{targetType.Name}" : $"{targetType.Name} t:TextAsset";
            string[] guids = AssetDatabase.FindAssets(searchFilter);

            if (guids.Length > 0)
            {
                var targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
                Selection.activeObject = targetObject;
                EditorGUIUtility.PingObject(targetObject);
            }
            else
            {
                Debug.LogWarning($"[TableReferenceChecker] {targetType.Name} 원본 파일을 찾을 수 없습니다.");
            }
        }
        #endregion
    }
}