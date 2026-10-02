#if UNITY_EDITOR
using CoreEngine.Settings;
using CoreEngine.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    public static class CoreEngineSettingsProvider
    {
        // ==============================================
        // 1. UI 스타일 전용 헬퍼 클래스 (코드 깔끔하게 분리)
        // ==============================================
        private class UIStyles
        {
            public GUIStyle mainTitle;
            public GUIStyle header;
            public GUIStyle desc;
            public GUIStyle box;

            public UIStyles()
            {
                mainTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 22, padding = new RectOffset(0, 0, 10, 15) };
                header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
                desc = new GUIStyle(EditorStyles.label) { fontSize = 13, wordWrap = true, richText = true };
                desc.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f);
                box = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(15, 15, 15, 15), margin = new RectOffset(0, 0, 0, 15) };
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateCoreEngineSettingsProvider()
        {
            var provider = new SettingsProvider("Project/CoreFramework", SettingsScope.Project)
            {
                label = "Core Framework",
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Core", "Global", "Scene", "Engine", "Extension", "Table", "Data" }),

                guiHandler = (searchContext) =>
                {
                    // [수정] 오류 원인 해결: autoSettings 인스턴스 로드 누락 추가
                    var settings = CoreEngineSettingsSO.Instance;
                    var autoSettings = CoreEngineAutoSettingsSO.Instance;

                    if (settings == null || autoSettings == null) return;

                    SerializedObject so = new SerializedObject(settings);
                    so.Update();

                    UIStyles styles = new UIStyles();

                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(15, 15, 10, 10) });
                    EditorGUILayout.LabelField("Core Engine Scene Settings", styles.mainTitle);

                    var sceneTracker = so.FindProperty("_sceneTracker");
                    if (sceneTracker == null)
                    {
                        EditorGUILayout.HelpBox("'_sceneTracker' 변수를 찾을 수 없습니다.", MessageType.Error);
                        EditorGUILayout.EndVertical();
                        return;
                    }

                    // ==============================================
                    // 2. 모듈화된 UI 그리기 함수 호출
                    // ==============================================
                    DrawSceneTrackerSections(sceneTracker, styles);
                    DrawDependenciesSection(so, settings, autoSettings, styles);

                    EditorGUILayout.EndVertical();
                    so.ApplyModifiedProperties();
                }
            };

            return provider;
        }

        // ==============================================
        // 3. UI 그리기 상세 구현부 (반복 코드 통합)
        // ==============================================

        private static void DrawSceneTrackerSections(SerializedProperty sceneTracker, UIStyles styles)
        {
            EditorGUILayout.BeginVertical(styles.box);
            DrawSectionBox("Global Scene",
                "※ 게임 시작부터 끝까지 유지되는 Scene",
                sceneTracker.FindPropertyRelative("globalScene"), styles, true);

            DrawSectionBox("Extension Scenes",
                "※ Global Scene 로드 시 순서대로 Additive되는 Scene들의 묶음\n※ Global Scene과 함께 게임 시작부터 끝까지 유지됨",
                sceneTracker.FindPropertyRelative("extensionSceneList"), styles, true);

            DrawSectionBox("First Scenes",
                "※ Global Scene과 Extension Scenes가 모두 로드된 후 최종적으로 로드 되는 Scene",
                sceneTracker.FindPropertyRelative("firstScene"), styles, true);
            EditorGUILayout.EndVertical();
        }

        // 중복되는 박스 그리기 로직을 하나의 헬퍼 함수로 통합
        private static void DrawSectionBox(string title, string description, SerializedProperty property, UIStyles styles, bool includeChildren = false)
        {
            EditorGUILayout.BeginVertical(styles.box);
            EditorGUILayout.LabelField(title, styles.header);
            GUILayout.Space(5);
            EditorGUILayout.LabelField(description, styles.desc);
            GUILayout.Space(10);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(property, new GUIContent(""), includeChildren);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private static void DrawDependenciesSection(SerializedObject so, CoreEngineSettingsSO settings, CoreEngineAutoSettingsSO autoSettings, UIStyles styles)
        {
            EditorGUILayout.BeginVertical(styles.box);
            EditorGUILayout.LabelField("Scene Table Dependencies", styles.header);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("※ TableAssetLoadManager가 부착된 씬들이 자동 감지됩니다.\n※ 각 씬에 진입할 때 메모리에 올릴 Table 목록을 설정하세요.", styles.desc);
            GUILayout.Space(10);

            SyncDependencies(settings, autoSettings);

            var depListProp = so.FindProperty("_sceneTableDependencies");
            if (depListProp == null)
            {
                EditorGUILayout.HelpBox("'_sceneTableDependencies' 프로퍼티를 찾을 수 없습니다.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            if (depListProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("현재 TableAssetLoadManager가 부착된 씬이 없습니다.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < depListProp.arraySize; i++)
                {
                    var element = depListProp.GetArrayElementAtIndex(i);
                    var sceneProp = element.FindPropertyRelative("targetScene");
                    var tablesProp = element.FindPropertyRelative("requiredTables");

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    var sceneName = sceneProp.FindPropertyRelative("sceneName").stringValue;
                    EditorGUILayout.LabelField($"📍 {sceneName} Scene", EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(tablesProp, new GUIContent("Preload Tables"), true);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // ==============================================
        // 4. 데이터 동기화 로직 (누락된 함수 추가)
        // ==============================================

        private static void SyncDependencies(CoreEngineSettingsSO settings, CoreEngineAutoSettingsSO autoSettings)
        {
            bool modified = false;
            foreach (var guid in autoSettings.ManagedSceneGUIDs)
            {
                if (settings.SceneTableDependencies.Find(x => x.targetSceneGUID == guid) == null)
                {
                    var newSceneRef = new SceneReference();
                    settings.AddDependency(guid, newSceneRef);

                    // 직렬화 객체를 통해 새로 추가된 리스트 항목의 SceneAsset 값을 강제 주입
                    var so = new SerializedObject(settings);
                    var listProp = so.FindProperty("_sceneTableDependencies");
                    var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                    var targetSceneProp = newElement.FindPropertyRelative("targetScene");
                    var assetProp = targetSceneProp.FindPropertyRelative("sceneAsset");

                    if (assetProp != null)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        assetProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        so.ApplyModifiedProperties();
                    }

                    modified = true;
                }
            }
            if (modified) AssetDatabase.SaveAssets();
        }
    }
}
#endif