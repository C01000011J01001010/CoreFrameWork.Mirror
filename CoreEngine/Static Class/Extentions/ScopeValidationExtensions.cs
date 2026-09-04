#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using CoreEngine.Settings;

namespace CoreEngine.Extensions
{
    public static class ScopeValidationExtensions
    {
        /// <summary>
        /// MonoBehaviour의 씬 소속을 판별하여 ContextScope를 자동 추론 및 할당합니다.
        /// </summary>
        public static void AutoSetupScope(this MonoBehaviour mono, ref ContextScope scope)
        {
#if UNITY_EDITOR
            // 씬에 존재하지 않거나 파괴 중이면 중단
            if (mono == null || !mono.gameObject.scene.IsValid()) return;

            string mySceneName = mono.gameObject.scene.name;
            bool isProjectScope = false;

            var settings = CoreEngineSettingsSO.Instance;
            if (settings != null)
            {
                // GlobalScene 또는 ExtensionSceneList에 포함되어 있는지 확인
                if (mySceneName == settings.GlobalScene) isProjectScope = true;
                else if (settings.ExtensionSceneList != null)
                {
                    foreach (var extScene in settings.ExtensionSceneList)
                    {
                        if (mySceneName == extScene) { isProjectScope = true; break; }
                    }
                }
            }

            // 현재 씬의 위치를 기반으로 '목표 스코프'를 계산
            ContextScope targetScope = ContextScope.None;
            if (isProjectScope) targetScope = ContextScope.Project;
            else if (!string.IsNullOrEmpty(mySceneName)) targetScope = ContextScope.Scene;

            // 현재 스코프가 목표 스코프와 '다를 때만' 값을 갱신하고 SetDirty 호출
            if (targetScope != ContextScope.None && scope != targetScope)
            {
                scope = targetScope;
                EditorUtility.SetDirty(mono); // 값이 실제로 바뀔 때만 1회 호출됨!
            }
#endif
        }
    }
}