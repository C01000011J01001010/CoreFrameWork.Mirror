using CoreEditor.Helpers;
using CoreEngine.GameData;
using CoreEngine.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CoreEditor.GameData
{
    public class PreloadAddressableSetter : EditorWindow
    {
        private List<ScriptableObject> _tableInstances = new List<ScriptableObject>();
        private List<ScriptableObject> _registryInstances = new List<ScriptableObject>();
        private Vector2 _scrollPosition;
        private string _searchQuery = ""; // [UX] 검색 필터

        public const string WindowName = "Preload Addressable Setter";
        [MenuItem(Constants.ToolRootGameData + WindowName, priority = Constants.GameDataPriority + 1)]
        private static void ShowWindow()
        {
            var window = GetWindow<PreloadAddressableSetter>(WindowName);
            window.minSize = GameDataNavigationHelper.TapSize;
            window.Show();
        }

        private void OnEnable()
        {
            ScanAndSyncTypes();
        }

        private void OnGUI()
        {
            GameDataNavigationHelper.DrawTopNavigationBar(GameDataNavigationHelper.Tab.PreloadSetter);

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

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 150f;

            PreloadAddressableSetterSettings.Instance.TargetGroupName = EditorGUILayout.TextField("대상 그룹 이름", PreloadAddressableSetterSettings.Instance.TargetGroupName);
            PreloadAddressableSetterSettings.Instance.TargetLabel = EditorGUILayout.TextField("부여할 라벨", PreloadAddressableSetterSettings.Instance.TargetLabel);

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(PreloadAddressableSetterSettings.Instance);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTypeToggles()
        {
            EditorGUILayout.LabelField("🎯 Addressable 등록 대상 타입 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();
            foreach (var state in PreloadAddressableSetterSettings.Instance.TypeStates)
            {
                state.IsEnabled = EditorGUILayout.ToggleLeft(state.TypeName, state.IsEnabled);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(PreloadAddressableSetterSettings.Instance);
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
                var state = PreloadAddressableSetterSettings.Instance.TypeStates.FirstOrDefault(t => t.TypeFullName == typeFullName);
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

            string[] tableGuids = AssetDatabase.FindAssets($"t:{nameof(_Table)}");
            foreach (string guid in tableGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null) { _tableInstances.Add(obj); foundTypes.Add(obj.GetType()); }
            }

            string[] registryGuids = AssetDatabase.FindAssets($"t:{nameof(_AssetRegistry)}");
            foreach (string guid in registryGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null) { _registryInstances.Add(obj); foundTypes.Add(obj.GetType()); }
            }

            _tableInstances = _tableInstances.OrderBy(i => i.GetType().Name).ThenBy(i => i.name).ToList();
            _registryInstances = _registryInstances.OrderBy(i => i.GetType().Name).ThenBy(i => i.name).ToList();

            PreloadAddressableSetterSettings.Instance.SyncTypes(foundTypes);
        }

        private void ExecuteBake()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
                return;
            }

            string groupName = PreloadAddressableSetterSettings.Instance.TargetGroupName;
            string targetLabel = PreloadAddressableSetterSettings.Instance.TargetLabel;

            // 1. 런타임 셋팅 SO에 라벨명 동기화
            if (CoreEngineAutoSettingsSO.Instance is IGameDataLabelSetter Setter)
            {
                Setter.SetGameDataLabel(targetLabel);
                EditorUtility.SetDirty(CoreEngineAutoSettingsSO.Instance);
            }

            // 2. 타겟 그룹 생성 또는 가져오기
            AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
            if (targetGroup == null)
            {
                targetGroup = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
                Debug.Log($"[Preload] '{groupName}' 그룹을 새로 생성했습니다.");
            }

            settings.AddLabel(targetLabel);

            int applyCount = 0;
            var allInstances = _tableInstances.Concat(_registryInstances);

            // [Fix] 기존에 속해있던 그룹들을 추적하기 위한 HashSet
            HashSet<AddressableAssetGroup> previousGroups = new HashSet<AddressableAssetGroup>();

            foreach (var instance in allInstances)
            {
                if (instance == null) continue;

                var state = PreloadAddressableSetterSettings.Instance.TypeStates.FirstOrDefault(t => t.TypeFullName == instance.GetType().FullName);
                if (state != null && state.IsEnabled)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instance));

                    // [Fix 2] 이동하기 전, 이 에셋이 원래 어느 그룹에 있었는지 기록해둠
                    var existingEntry = settings.FindAssetEntry(guid);
                    if (existingEntry != null && existingEntry.parentGroup != null)
                    {
                        previousGroups.Add(existingEntry.parentGroup);
                    }

                    // 에셋 이동 (또는 생성)
                    var entry = settings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);

                    // [Fix 1] 기존 라벨 싹 지우기 (초기화)
                    var existingLabels = entry.labels.ToList();
                    foreach (var oldLabel in existingLabels)
                    {
                        entry.SetLabel(oldLabel, false, true);
                    }

                    // 새로운 타겟 라벨만 단독으로 부여
                    entry.SetLabel(targetLabel, true, true);

                    applyCount++;
                }
            }

            // [Fix 2] 에셋 이동이 모두 끝난 후, 비어버린 예전 그룹들을 청소
            foreach (var oldGroup in previousGroups)
            {
                // 타겟 그룹이 아니고, 내부에 엔트리가 하나도 없으며, 어드레서블 기본 그룹이 아닐 경우에만 삭제
                if (oldGroup != null && oldGroup != targetGroup && oldGroup.entries.Count == 0 && !oldGroup.IsDefaultGroup())
                {
                    settings.RemoveGroup(oldGroup);
                    Debug.Log($"[Preload] 텅 빈 이전 그룹 '{oldGroup.Name}'을(를) 자동 삭제했습니다.");
                }
            }

            AssetDatabase.SaveAssets();

            this.ShowNotification(new GUIContent($"적용 완료! {applyCount}개의 SO 객체가 어드레서블에 등록되었습니다."));
        }
    }
}