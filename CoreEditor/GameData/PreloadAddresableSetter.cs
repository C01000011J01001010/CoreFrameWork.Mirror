using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using CoreEngine.GameData;

namespace CoreEditor.GameData
{
    public class PreloadAddresableSetter : EditorWindow
    {
        private List<ScriptableObject> _tableInstances = new List<ScriptableObject>();
        private List<ScriptableObject> _registryInstances = new List<ScriptableObject>();
        private Vector2 _scrollPosition;
        private string _searchQuery = ""; // [UX] 검색 필터

        public const string WindowName = "Preload Addresable Setter";
        [MenuItem(Constants.ToolRootGameData + WindowName, priority = Constants.GameDataPriority + 1)]
        private static void Open()
        {
            var window = GetWindow<PreloadAddresableSetter>(WindowName);
            window.minSize = new Vector2(550, 500);
            window.Show();
        }

        private void OnEnable()
        {
            ScanAndSyncTypes();
        }

        private void OnGUI()
        {
            DrawTopNavigationBar(); // [UX] 상단 탭 네비게이션

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("📦 Preload 메타데이터 어드레서블 관리", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawSettings();
            EditorGUILayout.Space();

            DrawTypeToggles();
            EditorGUILayout.Space();

            // [UX] 검색 바 추가
            _searchQuery = EditorGUILayout.TextField("🔍 검색 (파일명/클래스명)", _searchQuery, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space();

            DrawInstanceList();

            GUILayout.FlexibleSpace(); // [UX] 하단 고정 액션 바

            EditorGUILayout.Space();
            DrawBottomActions();
        }

        private void DrawTopNavigationBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("1. " + GameDataOrganizer.WindowName, EditorStyles.toolbarButton))
                GetWindow<GameDataOrganizer>(GameDataOrganizer.WindowName).Show();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("2. " + WindowName, EditorStyles.toolbarButton)) { }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("3. " + CsvToTableBatchProcessor.WindowName, EditorStyles.toolbarButton))
                GetWindow<CsvToTableBatchProcessor>(CsvToTableBatchProcessor.WindowName).Show();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 150f;

            PreloadAddresableSetterSettings.Instance.TargetGroupName = EditorGUILayout.TextField("대상 그룹 이름", PreloadAddresableSetterSettings.Instance.TargetGroupName);
            PreloadAddresableSetterSettings.Instance.TargetLabel = EditorGUILayout.TextField("부여할 라벨", PreloadAddresableSetterSettings.Instance.TargetLabel);

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(PreloadAddresableSetterSettings.Instance);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTypeToggles()
        {
            EditorGUILayout.LabelField("🎯 Addressable 등록 대상 타입 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();
            foreach (var state in PreloadAddresableSetterSettings.Instance.TypeStates)
            {
                state.IsEnabled = EditorGUILayout.ToggleLeft(state.TypeName, state.IsEnabled);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(PreloadAddresableSetterSettings.Instance);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawInstanceList()
        {
            _tableInstances.RemoveAll(i => i == null);
            _registryInstances.RemoveAll(i => i == null);

            int totalCount = _tableInstances.Count + _registryInstances.Count;
            EditorGUILayout.LabelField($"📄 발견된 SO 객체 목록 (총 {totalCount}개)", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");

            DrawInstanceSection("📊 Data Tables", _tableInstances);
            EditorGUILayout.Space(10);
            DrawInstanceSection("📇 Asset Registries", _registryInstances);

            EditorGUILayout.EndScrollView();
        }

        private void DrawInstanceSection(string title, List<ScriptableObject> instances)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal("toolbar");
            EditorGUILayout.LabelField("에셋 파일명 (File Name)", GUILayout.Width(250));
            EditorGUILayout.LabelField("클래스 이름 (Class Type)");
            EditorGUILayout.EndHorizontal();

            var filteredInstances = instances.Where(i =>
                string.IsNullOrEmpty(_searchQuery) ||
                i.name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                i.GetType().Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filteredInstances.Count == 0)
            {
                EditorGUILayout.HelpBox("조건에 맞는 에셋이 없습니다.", MessageType.None);
                return;
            }

            foreach (var instance in filteredInstances)
            {
                string typeFullName = instance.GetType().FullName;
                var state = PreloadAddresableSetterSettings.Instance.TypeStates.FirstOrDefault(t => t.TypeFullName == typeFullName);
                bool isEnabled = state != null && state.IsEnabled;

                GUI.enabled = isEnabled;
                EditorGUILayout.BeginHorizontal("helpbox");
                EditorGUILayout.LabelField(instance.name, GUILayout.Width(250));
                EditorGUILayout.LabelField(instance.GetType().Name);
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
            }
        }

        private void DrawBottomActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 프로젝트 스캔", GUILayout.Height(40)))
            {
                ScanAndSyncTypes();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("🚀 그룹 및 라벨 일괄 적용", GUILayout.Height(40)))
            {
                ExecuteBake();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void ScanAndSyncTypes()
        {
            _tableInstances.Clear();
            _registryInstances.Clear();
            var foundTypes = new HashSet<Type>();

            string[] tableGuids = AssetDatabase.FindAssets("t:_DataTable");
            foreach (string guid in tableGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null) { _tableInstances.Add(obj); foundTypes.Add(obj.GetType()); }
            }

            string[] registryGuids = AssetDatabase.FindAssets("t:_AssetRegistry");
            foreach (string guid in registryGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null) { _registryInstances.Add(obj); foundTypes.Add(obj.GetType()); }
            }

            _tableInstances = _tableInstances.OrderBy(i => i.GetType().Name).ThenBy(i => i.name).ToList();
            _registryInstances = _registryInstances.OrderBy(i => i.GetType().Name).ThenBy(i => i.name).ToList();

            PreloadAddresableSetterSettings.Instance.SyncTypes(foundTypes);
        }

        private void ExecuteBake()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
                return;
            }

            string groupName = PreloadAddresableSetterSettings.Instance.TargetGroupName;
            string targetLabel = PreloadAddresableSetterSettings.Instance.TargetLabel;

            AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
            if (targetGroup == null)
            {
                targetGroup = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            }

            settings.AddLabel(targetLabel);

            int applyCount = 0;
            var allInstances = _tableInstances.Concat(_registryInstances);

            foreach (var instance in allInstances)
            {
                if (instance == null) continue;

                var state = PreloadAddresableSetterSettings.Instance.TypeStates.FirstOrDefault(t => t.TypeFullName == instance.GetType().FullName);
                if (state != null && state.IsEnabled)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instance));
                    var entry = settings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);
                    entry.SetLabel(targetLabel, true, true);
                    applyCount++;
                }
            }

            AssetDatabase.SaveAssets();

            // [UX] 팝업 대신 Notification 사용
            this.ShowNotification(new GUIContent($"적용 완료! {applyCount}개의 SO 객체가 어드레서블에 등록되었습니다."));
        }
    }
}