#if UNITY_EDITOR
using CoreEngine.Settings;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    public static class CoreEngineSettingsProvider
    {

        // SettingsProvider 어트리뷰트를 달면 유니티 Project Settings 창에 자동으로 등록
        [SettingsProvider]
        public static SettingsProvider CreateCoreEngineSettingsProvider()
        {
            GUIStyle TitleFont = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20
            };

            GUIStyle LargeFont = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };

            var provider = new SettingsProvider("Project/CoreFramework", SettingsScope.Project)
            {
                // 좌측 메뉴에 표시될 이름
                label = "Core Framework",

                guiHandler = (searchContext) =>
                {

                    // Instance를 호출하는 순간 에셋이 없다면 즉시 생성됨
                    var settings = CoreEngineSettingsSO.Instance;
                    if (settings == null) return;

                    SerializedObject so = new SerializedObject(settings);
                    so.Update();

                    EditorGUILayout.Space();

                    var sceneTracker = so.FindProperty("_sceneTracker");
                    // 방어 코드: 누군가 원본 변수명을 바꿔서 찾지 못했을 경우, 에러 없이 친절한 안내 문구 출력
                    if (sceneTracker == null)
                    {
                        EditorGUILayout.HelpBox("[CoreEngineSettingsSO] 내부에 '_sceneTracker' 변수를 찾을 수 없습니다. 직렬화 변수명을 확인해주세요.", MessageType.Error);
                        return;
                    }

                    // ==============================================
                    // 코어 전역 씬 설정
                    // ==============================================

                    EditorGUILayout.LabelField("Global Scene (Project Scope)", TitleFont);
                    EditorGUILayout.LabelField("※ 게임 시작부터 끝까지 유지되는 Scene", LargeFont);
                    EditorGUILayout.PropertyField(sceneTracker.FindPropertyRelative("globalScene"), new GUIContent(""));

                    DrawLine();
                    //EditorGUILayout.Space(20);

                    // ==============================================
                    // 확장 시스템 씬 설정 (새로 추가된 부분)
                    // ==============================================
                    EditorGUILayout.LabelField("Extension Scenes (Project Scope)", TitleFont);
                    EditorGUILayout.LabelField("※ Global Scene 로드 시 순서대로 Additive되는 Scene들의 묶음", LargeFont);
                    EditorGUILayout.LabelField("※ Global Scene과 함께 게임 시작부터 끝까지 유지됨", LargeFont);

                    // 배열(Array)을 인스펙터에 전개하려면 세 번째 인자(includeChildren)를 반드시 true로 넘겨야 함
                    EditorGUILayout.PropertyField(sceneTracker.FindPropertyRelative("extensionSceneList"), new GUIContent(""), true);

                    DrawLine();
                    //EditorGUILayout.Space(20);

                    // 최종 Scene
                    EditorGUILayout.LabelField("First Scenes (Scene Scope)", TitleFont);
                    EditorGUILayout.LabelField("※ Global Scene과 Extension Scenes가 모두 로드된 후 최종적으로 로드 되는 Scene", LargeFont);
                    
                    EditorGUILayout.PropertyField(sceneTracker.FindPropertyRelative("firstScene"), new GUIContent(""));

                    so.ApplyModifiedProperties();
                },
                // 검색창에서 빠르게 찾을 수 있도록 "Extension" 키워드 추가
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Core", "Global", "Scene", "Engine", "Extension" })
            };

            return provider;
        }


        private static void DrawLine()
        {
            EditorGUILayout.Space(10);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(10);
        }
    }
}
#endif