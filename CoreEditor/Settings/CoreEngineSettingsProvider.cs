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
            var provider = new SettingsProvider("Project/CoreEngine", SettingsScope.Project)
            {
                // 좌측 메뉴에 표시될 이름
                label = "Core Engine",

                guiHandler = (searchContext) =>
                {
                    // Instance를 호출하는 순간 에셋이 없다면 즉시 생성됨
                    var settings = CoreEngineSettingsSO.Instance;
                    if (settings == null) return;

                    SerializedObject so = new SerializedObject(settings);
                    so.Update();

                    EditorGUILayout.Space();

                    // ==============================================
                    // 1. 코어 전역 씬 설정
                    // ==============================================
                    EditorGUILayout.LabelField("Framework System Scenes", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(so.FindProperty("globalScene"), new GUIContent("Global Scene"));

                    EditorGUILayout.Space(10);

                    // ==============================================
                    // 2. 확장 시스템 씬 설정 (새로 추가된 부분)
                    // ==============================================
                    EditorGUILayout.LabelField("Extension System Scenes", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("GlobalScene 로드 시 Additive로 자동 병합될 확장 패키지(예: FishNet)의 씬들을 등록합니다.", MessageType.Info);

                    // 💡 배열(Array)을 인스펙터에 전개하려면 세 번째 인자(includeChildren)를 반드시 true로 넘겨야 합니다.
                    EditorGUILayout.PropertyField(so.FindProperty("ExtensionSceneList"), new GUIContent("Extension Scenes"), true);

                    so.ApplyModifiedProperties();
                },
                // 검색창에서 빠르게 찾을 수 있도록 "Extension" 키워드 추가
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Core", "Global", "Scene", "Engine", "Extension" })
            };

            return provider;
        }
    }
}
#endif