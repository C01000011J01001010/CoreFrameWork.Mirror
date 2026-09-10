using CoreEngine;
using CoreEngine.Helpers;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoreEngine.Director;
using CoreEngine.EventBus;

namespace CoreEngine
{
    //public struct SceneReadyEvent : IEvent { }

    /// <summary>
    /// Additive로 로드되는 개별 씬마다 존재하는 컨텍스트
    /// 씬이 언로드될 때 자연스럽게 파괴되며 메모리를 정리
    /// </summary>
    [DefaultExecutionOrder((int)ExecutionOrder.SceneContext)]
    public class SceneContext : BaseContext<SceneContext>
    {
        protected override ContextScope myScope => ContextScope.Scene;

        [Tooltip("Scene이 완전히 초기화 된 후 ActiveScene으로 설정할지 결정")]
        [SerializeField]
        private bool isActiveScene = true;

        protected override void Awake()
        {
            base.Awake();

            // 💡 유저님의 아이디어: 내 게임오브젝트가 속한 씬 프로퍼티를 그대로 디렉터에게 패스!
            Scene currentSceneDomain = gameObject.scene;

            if (isActiveScene)
            {
                // SceneFlowDirector에게 이 씬이 현재 활성화된 메인 도메인임을 선언
                SceneFlowDirector.RegisterCurrentScene(currentSceneDomain);
            }
        }

        public override IEnumerator Initialize()
        {
            LogHelper.LogFunctionCallCount(this);

            // 부모(BaseContext)의 전체 초기화 시퀀스를 먼저 완주합니다. 
            // (ManagerHub -> ActorHubs -> UiHub 순차 로드 완료 대기)
            yield return base.Initialize();

            // 1프레임 쉬어주고
            yield return null;

            // Scene의 초기화가 종료되었음을 알림
            _isInit = true;

            // 모니터링 하는 객체에서 지연 초기화가 가능하도록 제어를 넘김
            yield return null; 
            
            // 모든 씬 객체의 세팅이 끝난 타이밍에 Update 가동
            UpdateDirector.StartTicking();

        }

        protected override void OnDestroy()
        {
            UpdateDirector.StopTicking();
            base.OnDestroy();
        }
    }
}

