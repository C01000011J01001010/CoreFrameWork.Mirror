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
    public class PreloadDashboard : EditorWindow
    {
        private List<ScriptableObject> _tableInstances = new List<ScriptableObject>();
        private List<ScriptableObject> _registryInstances = new List<ScriptableObject>();
        private Vector2 _scrollPosition;

        [MenuItem(Constants.ToolRootGameData + "Preload Data Manager")]
        private static void Open()
        {
            var window = GetWindow<PreloadDashboard>("Preload Manager");
            window.minSize = new Vector2(550, 500);
            window.Show();
        }

        private void OnEnable()
        {
            ScanAndSyncTypes();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("📦 Preload 메타데이터 어드레서블 관리", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawSettings();
            EditorGUILayout.Space();

            DrawTypeToggles();
            EditorGUILayout.Space();

            DrawActionButtons();
            EditorGUILayout.Space();

            DrawInstanceList();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 150f;

            PreloadSettings.Instance.TargetGroupName = EditorGUILayout.TextField("대상 그룹 이름", PreloadSettings.Instance.TargetGroupName);
            PreloadSettings.Instance.TargetLabel = EditorGUILayout.TextField("부여할 라벨", PreloadSettings.Instance.TargetLabel);

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(PreloadSettings.Instance);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTypeToggles()
        {
            EditorGUILayout.LabelField("🎯 Addressable 등록 대상 타입 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();
            foreach (var state in PreloadSettings.Instance.TypeStates)
            {
                state.IsEnabled = EditorGUILayout.ToggleLeft(state.TypeName, state.IsEnabled);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(PreloadSettings.Instance);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 프로젝트 스캔", GUILayout.Height(30)))
            {
                ScanAndSyncTypes();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("🚀 그룹 및 라벨 일괄 적용", GUILayout.Height(30)))
            {
                ExecuteBake();
            }
            GUI.backgroundColor = Color.white;

            // 툴 간 빠른 이동 버튼
            if (GUILayout.Button("➡️ SO Data Validator 열기", GUILayout.Height(30), GUILayout.Width(170)))
            {
                GetWindow<SODataValidator>("SO Data Validator").Show();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInstanceList()
        {
            // [Fix] 그리기 전 파괴된 객체(Null) 정리
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

            if (instances.Count == 0)
            {
                EditorGUILayout.HelpBox("해당 타입의 에셋이 발견되지 않았습니다.", MessageType.None);
                return;
            }

            foreach (var instance in instances)
            {
                if (instance == null) continue; // 안전 장치

                string typeFullName = instance.GetType().FullName;
                var state = PreloadSettings.Instance.TypeStates.FirstOrDefault(t => t.TypeFullName == typeFullName);
                bool isEnabled = state != null && state.IsEnabled;

                GUI.enabled = isEnabled;
                EditorGUILayout.BeginHorizontal("helpbox");
                EditorGUILayout.LabelField(instance.name, GUILayout.Width(250));
                EditorGUILayout.LabelField(instance.GetType().Name);
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
            }
        }

        private void ScanAndSyncTypes()
        {
            _tableInstances.Clear();
            _registryInstances.Clear();
            var foundTypes = new HashSet<Type>();

            // 테이블 스캔
            string[] tableGuids = AssetDatabase.FindAssets("t:_DataTable");
            foreach (string guid in tableGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null)
                {
                    _tableInstances.Add(obj);
                    foundTypes.Add(obj.GetType());
                }
            }

            // 레지스트리 스캔
            string[] registryGuids = AssetDatabase.FindAssets("t:_AssetRegistry");
            foreach (string guid in registryGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null)
                {
                    _registryInstances.Add(obj);
                    foundTypes.Add(obj.GetType());
                }
            }

            // 이름순 정렬
            _tableInstances = _tableInstances.OrderBy(i => i.GetType().Name).ThenBy(i => i.name).ToList();
            _registryInstances = _registryInstances.OrderBy(i => i.GetType().Name).ThenBy(i => i.name).ToList();

            PreloadSettings.Instance.SyncTypes(foundTypes);
        }

        private void ExecuteBake()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
                return;
            }

            string groupName = PreloadSettings.Instance.TargetGroupName;
            string targetLabel = PreloadSettings.Instance.TargetLabel;

            AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
            if (targetGroup == null)
            {
                targetGroup = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
                Debug.Log($"[Preload] '{groupName}' 그룹을 새로 생성했습니다.");
            }

            settings.AddLabel(targetLabel);

            int applyCount = 0;
            var allInstances = _tableInstances.Concat(_registryInstances);

            foreach (var instance in allInstances)
            {
                if (instance == null) continue; // 안전 장치

                var state = PreloadSettings.Instance.TypeStates.FirstOrDefault(t => t.TypeFullName == instance.GetType().FullName);
                if (state != null && state.IsEnabled)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instance));
                    var entry = settings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);
                    entry.SetLabel(targetLabel, true, true);
                    applyCount++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("적용 완료", $"선택된 {applyCount}개의 SO 에셋이 '{groupName}' 그룹에 편입되고 '{targetLabel}' 라벨이 부여되었습니다.", "확인");
        }
    }
}