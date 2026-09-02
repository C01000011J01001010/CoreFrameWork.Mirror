#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using CoreEngine.Settings;

namespace CoreEngine.Extentions
{
    public static class ScopeValidationExtensions
    {
        /// <summary>
        /// MonoBehaviour의 씬 소속을 판별하여 ContextScope를 자동 추론 및 할당합니다.
        /// </summary>
        public static void AutoSetupScope(this MonoBehaviour mono, ref ContextScope scope)
        {
#if UNITY_EDITOR
            // 컴파일/파괴 중이거나 씬에 존재하지 않는 프리팹 에셋일 경우 로직 중단
            if (mono == null || !mono.gameObject.scene.IsValid()) return;

            // 아직 스코프가 None(미지정) 상태일 때만 자동 추론 작동
            if (scope == ContextScope.None)
            {
                string mySceneName = mono.gameObject.scene.name;
                bool isProjectScope = false;

                // 에디터 타임에 전역 설정 SO 에셋 로드
                var settings = CoreEngineSettingsSO.Instance;

                if (settings != null)
                {
                    // 메인 GlobalScene 검사
                    if (mySceneName == settings.GlobalScene)
                    {
                        isProjectScope = true;
                    }
                    // 확장 씬(ExtensionSceneList) 배열 순회 검사
                    else if (settings.ExtensionSceneList != null)
                    {
                        foreach (var extScene in settings.ExtensionSceneList)
                        {
                            if (mySceneName == extScene)
                            {
                                isProjectScope = true;
                                break; // 하나라도 일치하면 즉시 탈출
                            }
                        }
                    }
                }

                // 판별 결과에 따른 스코프 자동 지정
                if (isProjectScope)
                {
                    scope = ContextScope.Project;
                    EditorUtility.SetDirty(mono);
                }
                else if (!string.IsNullOrEmpty(mySceneName))
                {
                    scope = ContextScope.Scene;
                    EditorUtility.SetDirty(mono);
                }
            }
#endif
        }
    }
}