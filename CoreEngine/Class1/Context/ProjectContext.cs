using CoreEngine.Director;
using CoreEngine.EventBus;
using CoreEngine.Helpers;
using CoreEngine.Loading;
using CoreEngine.SceneManagement;
using CoreEngine.Settings;
using CoreEngine.Test;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreEngine
{

    public struct ProjectContextProgressEvent : IEvent
    {
        // 로딩 화면에 띄워줄 메시지 (예: "Global Managers 초기화 중...")
        public string Message;

        // 0.0f ~ 1.0f 사이의 진행도 (필요 없다면 제거 가능)
        public float Progress;

        public ProjectContextProgressEvent(string message, float progress)
        {
            Message = message;
            Progress = progress;
        }
    }

    /// <summary>
    /// GlobalScene에 상주하며 게임 종료 시까지 파괴되지 않는 전역 컨텍스트
    /// GlobalScene은 Additive 방식으로 언로드되지 않으므로 
    /// 자연스럽게 앱 종료 시점까지 생존이 보장
    /// </summary>
    [DefaultExecutionOrder((int)ExecutionOrder.ProjectContext)]
    public class ProjectContext : BaseContext<ProjectContext>
    {
        
        [Tooltip("GlobalScene에서 시작시 최종적으로 로드할 씬"),SerializeField] private SceneReference _firstScene;
        public static SceneReference FirstScene => Inst?._firstScene;

        private CoreEngineSettingsSO CoreEngineSettings => CoreEngineSettingsSO.Instance;

        protected override ContextScope myScope => ContextScope.Project;

        private IEnumerator Start()
        {
            LogHelper.LogFunctionCallCount(this);

            // 코어 매니저들을 초기화하기 전, 확장 시스템 씬들을 먼저 런타임에 병합합니다.
            yield return LoadExtensionScenesRoutine();

            // BaseContext의 초기화를 실행 (내부에서 0.3, 0.6, 0.9 순서로 이벤트가 발송됨)
            yield return Initialize();

            // 모든 전역 시스템 세팅이 끝났으므로, 첫 씬을 로드하라고 허공에 외침 (EventBus)
            
            if(TestDriver.IsSceneTest)
            {
                Debug.Log($"[ProjectContext] 단독 씬 테스트 환경 시스템 빌드업을 시작합니다.");

                // 전용 이벤트를 발행하여 디렉터의 공통 파이프라인(하단부)을 태움
                EventBus<SceneTestBootstrapRequestEvent>.Publish(new SceneTestBootstrapRequestEvent(TestDriver.TestScene));
            }
            else
            {
                EventBus<SceneLoadRequestEvent>.Publish(new SceneLoadRequestEvent(FirstScene));
            }
        }

        /// <summary>
        /// 설정에 등록된 모든 확장 씬을 비동기로 GlobalScene 공간에 병합합니다.
        /// </summary>
        private IEnumerator LoadExtensionScenesRoutine()
        {
            if (CoreEngineSettings == null || CoreEngineSettings.ExtensionSceneList == null)
            {
                yield break;
            }

            foreach (var extScene in CoreEngineSettings.ExtensionSceneList)
            {
                string sceneName = extScene.SceneName;
                if (!string.IsNullOrEmpty(sceneName))
                {
                    Debug.Log($"[ProjectContext] 확장 시스템 씬 로드 시작: {sceneName}");

                    AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                    // 메모리에 완전히 올라갈 때까지 대기
                    while (!asyncOp.isDone)
                    {
                        yield return null;
                    }

                    // 이 코루틴 대기가 끝난 시점에는 확장 씬 내부 객체들의 Awake와 OnEnable이 이미 실행 완료된 상태
                    // 즉, 확장 시스템들이 CoreFacade를 통해 전역 허브에 무사히 합류했음을 보장
                    Debug.Log($"[ProjectContext] 확장 시스템 등록 완료: {sceneName}");
                }
            }
        }
    }
}
