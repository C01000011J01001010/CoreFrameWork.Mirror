using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;
using CoreEditor.Helpers;

namespace CoreEditor.GameData
{
    public class CsvToTableBatchProcessor : EditorWindow
    {
        private class TableProcessInfo
        {
            public bool IsSelected = true;
            public _Table TableAsset;
            public TextAsset MatchedCsv;
            public string StatusMessage = "대기 중";
            public MessageType StatusType = MessageType.None;
            public List<TextAsset> ConflictedCsvs = new List<TextAsset>();
        }

        private List<TableProcessInfo> _tableInfos = new List<TableProcessInfo>();
        private Vector2 _scrollPosition;
        private string _searchQuery = "";

        private string tableTypeName = nameof(_Table);
        public const string WindowName = "CSV To Table Batch Processor";

        [MenuItem(Constants.ToolRootGameData + WindowName, priority = Constants.GameDataPriority + 2)]
        private static void Open()
        {
            var window = GetWindow<CsvToTableBatchProcessor>(WindowName);
            window.minSize = new Vector2(600, 400);
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

        #region 스캐닝 및 배치 처리 위임
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
                    info.StatusMessage = $"이름이 동일한 CSV가 여러 개({validCsvs.Count}개) 발견되었습니다. 위치를 확인하세요.";
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
                    if (tableSetter == null) throw new InvalidOperationException("ITableSetter 인터페이스 미구현");

                    Type recordType = GetRecordType(info.TableAsset.GetType());

                    // 💡 실제 변환 처리는 파이프라인 클래스로 위임!
                    CsvImportPipeline.ProcessTable(recordType, tableSetter, info.MatchedCsv);

                    info.StatusMessage = "✅ 변환 성공";
                    info.StatusType = MessageType.Info;
                    successCount++;
                }
                catch (System.Reflection.TargetInvocationException ex)
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
        #endregion
    }
}