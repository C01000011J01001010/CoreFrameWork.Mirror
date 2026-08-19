using System.Collections;
using UnityEngine;
using CoreEngine.EventBus;
using CoreEngine.Test;
using CoreEngine.Director;
using CoreEngine.Loading;
using CoreEngine.SceneManagement;

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
        [SerializeField] private SceneReference _globalScene;
        public static SceneReference GlobalScene => Inst._globalScene;

        [SerializeField] private SceneReference _firstScene;
        public static SceneReference FirstScene => Inst._firstScene;

        protected override ContextScope myScope => ContextScope.Project;

        private IEnumerator Start()
        {
            UtilityLog.LogFunctionCallCount(this);

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
    }
}
