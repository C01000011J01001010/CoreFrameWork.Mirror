using CoreEngine.EventBus;
// SystemLoadingEvent가 있는 네임스페이스 추가 (필요시 수정)
using CoreEngine.Hub;
using System.Collections;
using UnityEngine;
using CoreEngine.DesignPattern.Singleton;

namespace CoreEngine
{
    public enum ContextScope
    {
        None, // 범위 상관없음
        Project, // GlobalScene에 있는 ProjectContext 산하
        Scene, // 개별Scene에 있는 SceneContext 산하
    }

    public interface IContextScope
    {
        public ContextScope scope { get; }
    }

    [RequireComponent(typeof(ManagerHub))]
    [RequireComponent(typeof(ActorHub))] // 💡 단일 ActorHub도 필수로 요구하도록 추가!
    [RequireComponent(typeof(UiHub))]
    public abstract class BaseContext<T> : Singleton<T> 
        where T : BaseContext<T>
    {
        //protected static T _instance;
        //public static T Inst => _instance;

        private bool _isInit;
        public static bool IsInit => Inst._isInit;

        protected abstract ContextScope myScope { get; }

        [Header("Hubs (2계층)")]
        internal ManagerHub managerHub { get; private set; }
        internal ActorHub actorHub { get; private set; }
        internal UiHub uiHub { get; private set; }

        protected override void OnDestroy()
        {
            OnDestroyToss();
            base.OnDestroy();
        }

        protected override void Awake()
        {
            base.Awake();

            // GetOrAddComponent로 안전하게 Hub들을 가져오거나 생성
            managerHub = gameObject.GetComponent<ManagerHub>();
            actorHub = gameObject.GetComponent<ActorHub>();
            uiHub = gameObject.GetComponent<UiHub>();

            // 허브의 scope를 Context랑 동일하게 맞춤
            managerHub.SetScope(myScope);
            actorHub.SetScope(myScope);
            uiHub.SetScope(myScope);

            AwakeToss();
        }

        public void AwakeToss()
        {
            // 가장 처음 시작하는 Context가 책임지고 Hub를 Awake (구독 시작)
            // Manager -> Actor -> UI 순서 명확화
            managerHub?.AwakeFromContext();
            actorHub?.AwakeFromContext();
            uiHub?.AwakeFromContext();
        }

        public void OnDestroyToss()
        {
            // 초기화의 반대 순서로 정리될수 있도록 보장
            uiHub?.OnDestroyFromContext();
            actorHub?.OnDestroyFromContext();
            managerHub?.OnDestroyFromContext();
        }

        public virtual IEnumerator Initialize()
        {
            yield return InitializeHubsSequence();
        }

        protected virtual IEnumerator InitializeHubsSequence()
        {
            // 0. 로딩 시작 알림
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Start, "시스템 초기화 준비...", 0f));

            if (gameObject.name == "SceneContext")
            {
                Debug.Log("SceneContext 허브 초기화");
            }

            // 1. ManagerHub 초기화 (1:1 시스템 매니저들 서비스 세팅)
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "매니저 시스템 로드 중...", 0.33f));
            yield return managerHub.Initialize();

            // 2. 단일 ActorHub 초기화 
            // 💡 더 이상 List 루프를 돌지 않고, 단일 객체만 가볍게 초기화합니다.
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "인게임 엔티티(Actor) 시스템 세팅 중...", 0.66f));
            yield return actorHub.Initialize();

            // 3. UiHub 초기화 (안전하게 모든 데이터가 완비된 시점에 뷰 로드)
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "UI 시스템 로드 중...", 0.9f));
            yield return uiHub.Initialize();

            #region LateInit
            // LateInit 역시 루프 없이 깔끔하게 1:1:1 호출
            yield return managerHub.LateInitialize();
            yield return actorHub.LateInitialize();
            yield return uiHub.LateInitialize();
            #endregion

            // 4. 로딩 완료 알림
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Complete, "초기화 완료!", 1.0f));

            _isInit = true;
        }
    }
}