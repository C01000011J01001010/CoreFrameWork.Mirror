#if UNITY_EDITOR
using CoreEngine.Settings;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    public static class CoreEngineSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateCoreEngineSettingsProvider()
        {
            var provider = new SettingsProvider("Project/CoreFramework", SettingsScope.Project)
            {
                label = "Core Framework",

                guiHandler = (searchContext) =>
                {
                    // ==============================================
                    // 1. UI 스타일 세팅 (에러 방지를 위해 핸들러 내부에서 초기화)
                    // ==============================================
                    GUIStyle mainTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 22,
                        padding = new RectOffset(0, 0, 10, 15)
                    };

                    GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 18
                    };

                    GUIStyle descStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 13,
                        wordWrap = true,
                        richText = true
                    };
                    // 다크/라이트 테마에 맞춰 텍스트 색상 자동 조정
                    descStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f);

                    // 각 구역을 묶어줄 깔끔한 박스 스타일
                    GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(15, 15, 15, 15),
                        margin = new RectOffset(0, 0, 0, 15)
                    };

                    // ==============================================
                    // 2. 데이터 세팅 및 유효성 검사
                    // ==============================================
                    var settings = CoreEngineSettingsSO.Instance;
                    if (settings == null) return;

                    SerializedObject so = new SerializedObject(settings);
                    so.Update();

                    // 전체 화면 좌우 패딩을 주어 답답하지 않게 배치
                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(15, 15, 10, 10) });

                    EditorGUILayout.LabelField("Core Engine Scene Settings", mainTitleStyle);

                    var sceneTracker = so.FindProperty("_sceneTracker");
                    if (sceneTracker == null)
                    {
                        EditorGUILayout.HelpBox("[CoreEngineSettingsSO] 내부에 '_sceneTracker' 변수를 찾을 수 없습니다. 직렬화 변수명을 확인해주세요.", MessageType.Error);
                        EditorGUILayout.EndVertical();
                        return;
                    }

                    // ==============================================
                    // 3. 코어 전역 씬 설정
                    // ==============================================
                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUILayout.LabelField("Global Scene", headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("※ 게임 시작부터 끝까지 유지되는 Scene", descStyle);
                    GUILayout.Space(10);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sceneTracker.FindPropertyRelative("globalScene"), new GUIContent(""));
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                    // ==============================================
                    // 4. 확장 시스템 씬 설정
                    // ==============================================
                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUILayout.LabelField("Extension Scenes", headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("※ Global Scene 로드 시 순서대로 Additive되는 Scene들의 묶음\n※ Global Scene과 함께 게임 시작부터 끝까지 유지됨", descStyle);
                    GUILayout.Space(10);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sceneTracker.FindPropertyRelative("extensionSceneList"), new GUIContent(""), true);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                    // ==============================================
                    // 5. 최종 씬 설정
                    // ==============================================
                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUILayout.LabelField("First Scenes", headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("※ Global Scene과 Extension Scenes가 모두 로드된 후 최종적으로 로드 되는 Scene", descStyle);
                    GUILayout.Space(10);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sceneTracker.FindPropertyRelative("firstScene"), new GUIContent(""));
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndVertical(); // 전체 패딩 종료

                    so.ApplyModifiedProperties();
                },

                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Core", "Global", "Scene", "Engine", "Extension" })
            };

            return provider;
        }
    }
}
#endif