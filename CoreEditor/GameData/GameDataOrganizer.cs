using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;

namespace CoreEditor.GameData
{
    public class GameDataOrganizer : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _searchQuery = ""; // [UX] 검색 쿼리

        private List<TypeStatus> _tableStatuses = new();
        private List<TypeStatus> _registryStatuses = new();

        private const string TableFolderName = "Table";
        private const string RegistryFolderName = "Registry";

        public const string WindowName = "GameData Organizer";
        [MenuItem(Constants.ToolRootGameData + WindowName, priority = Constants.GameDataPriority + 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameDataOrganizer>(WindowName);
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            DrawTopNavigationBar(); // [UX] 공통 상단 탭

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Game Data SO 대시보드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"{nameof(_Table)} 및 {nameof(_AssetRegistry)}를 상속받은 상세 클래스들의 SO 객체 상태를 확인하고 관리합니다.", MessageType.Info);
            EditorGUILayout.Space();

            DrawPathSettings();
            EditorGUILayout.Space();

            // [UX] 검색 필터
            _searchQuery = EditorGUILayout.TextField("🔍 검색 (클래스명)", _searchQuery, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawSection("📊 Tables", _tableStatuses);
            EditorGUILayout.Space();
            DrawSection("📇 Registries", _registryStatuses);
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace(); // [UX] 남은 공간을 밀어내어 버튼을 하단에 고정

            EditorGUILayout.Space();
            DrawBottomActions();
        }

        // [UX] 상단 네비게이션 탭
        private void DrawTopNavigationBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.backgroundColor = Color.cyan; // 현재 탭 하이라이트
            if (GUILayout.Button("1. " + WindowName, EditorStyles.toolbarButton)) { }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("2. " + PreloadAddressableSetter.WindowName, EditorStyles.toolbarButton))
                GetWindow<PreloadAddressableSetter>(PreloadAddressableSetter.WindowName).Show();
            if (GUILayout.Button("3. " + CsvToTableBatchProcessor.WindowName, EditorStyles.toolbarButton))
                GetWindow<CsvToTableBatchProcessor>(CsvToTableBatchProcessor.WindowName).Show();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPathSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📁 저장 경로 설정 (탐색기 선택)", EditorStyles.boldLabel);

            var settings = GameDataOrganizerSettings.Instance;
            EditorGUI.BeginChangeCheck();
            DrawFolderPickerRow("기본 저장 경로", ref settings.BaseSaveDirectory);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            GUI.contentColor = Color.gray;
            EditorGUILayout.LabelField($" └─ Table 저장 경로: {settings.BaseSaveDirectory}/{TableFolderName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($" └─ Registry 저장 경로: {settings.BaseSaveDirectory}/{RegistryFolderName}", EditorStyles.miniLabel);
            GUI.contentColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawFolderPickerRow(string label, ref string savedDirectory)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(savedDirectory);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("📂", GUILayout.Width(30)))
            {
                string startDirectory = Application.dataPath;
                if (!string.IsNullOrEmpty(savedDirectory))
                {
                    string projectDirectory = Path.GetDirectoryName(Application.dataPath);
                    startDirectory = $"{projectDirectory}/{savedDirectory}";
                }

                

                string absolutePath = EditorUtility.OpenFolderPanel($"{label} 선택", startDirectory, "");
                if (!string.IsNullOrEmpty(absolutePath))
                {
                    if (absolutePath.StartsWith(Application.dataPath))
                    {
                        savedDirectory = "Assets" + absolutePath.Substring(Application.dataPath.Length);
                        GUI.FocusControl(null);
                    }
                    else
                    {
                        Debug.LogWarning($"[{WindowName}] 프로젝트 내부(Assets 하위)의 폴더만 선택할 수 있습니다.");
                    }
                }
            }

            if (GUILayout.Button("확인", GUILayout.Width(50)))
            {
                PingFolder(savedDirectory);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(string title, List<TypeStatus> statuses)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // [UX] 검색 필터 적용
            var filteredStatuses = statuses.Where(s => string.IsNullOrEmpty(_searchQuery) ||
                                                       s.TargetType.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filteredStatuses.Count == 0)
            {
                EditorGUILayout.LabelField(statuses.Count == 0 ? "  발견된 상세 클래스가 없습니다." : "  검색 결과가 없습니다.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var status in filteredStatuses)
                {
                    DrawStatusRow(status);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(TypeStatus status)
        {
            EditorGUILayout.BeginHorizontal();
            string icon = status.InstanceCount == 1 ? "✅" : (status.InstanceCount == 0 ? "❌" : "⚠️");
            EditorGUILayout.LabelField($"{icon} {status.TargetType.Name}", GUILayout.Width(250));

            if (status.InstanceCount == 0)
            {
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("객체 없음 (생성 필요)");
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else if (status.InstanceCount == 1)
            {
                GUI.contentColor = Color.green;
                EditorGUILayout.LabelField("정상");
                GUI.contentColor = Color.white;
                if (GUILayout.Button("선택", GUILayout.Width(50))) PingAsset(status.AssetPaths[0]);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField($"중복! ({status.InstanceCount}개 존재)");
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                for (int i = 0; i < status.AssetPaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.contentColor = Color.gray;
                    EditorGUILayout.LabelField($"└─ {status.AssetPaths[i]}", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;
                    if (GUILayout.Button("선택", GUILayout.Width(50))) PingAsset(status.AssetPaths[i]);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawBottomActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 전체 상태 새로고침", GUILayout.Height(40)))
            {
                Refresh();
            }

            var missingCount = _tableStatuses.Count(s => s.InstanceCount == 0) + _registryStatuses.Count(s => s.InstanceCount == 0);
            GUI.enabled = missingCount > 0;
            GUI.backgroundColor = missingCount > 0 ? Color.cyan : Color.gray;
            if (GUILayout.Button($"🚀 누락된 에셋 일괄 생성하기 ({missingCount}개)", GUILayout.Height(40)))
            {
                GenerateMissingAssets();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            var tableTypes = TypeCache.GetTypesDerivedFrom<_Table>().Where(t => !t.IsAbstract && !t.IsGenericType).ToList();
            var registryTypes = TypeCache.GetTypesDerivedFrom<_AssetRegistry>().Where(t => !t.IsAbstract && !t.IsGenericType).ToList();

            _tableStatuses = AnalyzeTypes(tableTypes);
            _registryStatuses = AnalyzeTypes(registryTypes);
        }

        private List<TypeStatus> AnalyzeTypes(List<Type> types)
        {
            var result = new List<TypeStatus>();
            foreach (var type in types)
            {
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
                result.Add(new TypeStatus
                {
                    TargetType = type,
                    InstanceCount = guids.Length,
                    AssetPaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList()
                });
            }
            return result.OrderBy(s => s.TargetType.Name).ToList();
        }

        private void GenerateMissingAssets()
        {
            var settings = GameDataOrganizerSettings.Instance;
            string tablePath = $"{settings.BaseSaveDirectory}/{TableFolderName}";
            string registryPath = $"{settings.BaseSaveDirectory}/{RegistryFolderName}";

            EnsureDirectoryExists(tablePath);
            EnsureDirectoryExists(registryPath);

            int missingCount = _tableStatuses.Count(s => s.InstanceCount == 0) + _registryStatuses.Count(s => s.InstanceCount == 0);

            GenerateForStatuses(_tableStatuses, tablePath);
            GenerateForStatuses(_registryStatuses, registryPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Refresh();

            // [UX] 비동기 알림 (팝업 없음)
            this.ShowNotification(new GUIContent($"작업 완료! {missingCount}개의 누락된 에셋이 생성되었습니다."));
        }

        private void GenerateForStatuses(List<TypeStatus> statuses, string targetPath)
        {
            foreach (var status in statuses.Where(s => s.InstanceCount == 0))
            {
                var instance = ScriptableObject.CreateInstance(status.TargetType);
                string assetPath = $"{targetPath}/{status.TargetType.Name}.asset";
                AssetDatabase.CreateAsset(instance, assetPath);
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private void PingFolder(string path)
        {
            EnsureDirectoryExists(path);
            AssetDatabase.Refresh();
            UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            if (folderObj != null)
            {
                Selection.activeObject = folderObj;
                EditorGUIUtility.PingObject(folderObj);
            }
        }

        private void PingAsset(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private class TypeStatus
        {
            public Type TargetType;
            public int InstanceCount;
            public List<string> AssetPaths;
        }
    }
}