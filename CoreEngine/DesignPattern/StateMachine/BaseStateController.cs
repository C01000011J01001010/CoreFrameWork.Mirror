using CoreEngine.Actor;
using CoreEngine.Facades;
using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine
{
    // CRTP 패턴 적용: 자식 컨트롤러의 타입을 TController로 스스로 넘겨줌
    public abstract class BaseStateController<TState, TManager, TController> : BaseActorFeature
        where TState : struct, Enum
        where TManager : BaseStateManager<TState, TController>
        where TController : BaseStateController<TState, TManager, TController>
    {
        [SerializeField] 
        protected TState defaultStateType;

        protected TState currentStateType;

        // 매니저에서 받아올 현재 상태 로직 (Stateless)
        protected IState<TState, TController> CurrentState;

        // 매니저 캐싱
        protected TManager _stateManager;

        // GC(가비지 컬렉션) 발생을 원천 차단하기 위한 정적 캐싱
        private static readonly EqualityComparer<TState> Comparer = EqualityComparer<TState>.Default;

        public TState CurrentStateType => currentStateType;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // 전역 코어 파사드에서 해당 매니저를 가져와 캐싱
            _stateManager = CoreFacade.GetManager<TManager>();
        }

        public void StartState()
        {
            currentStateType = defaultStateType;
            CurrentState = _stateManager.GetState(defaultStateType);

            CurrentState?.Enter((TController)this);
        }

        public virtual void StopState()
        {
            CurrentState?.Exit((TController)this, null);
            CurrentState = null;
        }

        public virtual void Tick(float deltaTime)
        {
            if (CurrentState == null) return;

            TState? nextState = CurrentState.CheckTransitions((TController)this);

            if (nextState.HasValue)
            {
                TransitionTo(nextState.Value);
                return;
            }
            CurrentState.Update((TController)this, deltaTime);
        }

        public virtual void FixedTick(float fixedDeltaTime)
        {
            CurrentState?.FixedUpdate((TController)this, fixedDeltaTime);
        }

        protected virtual void TransitionTo(TState nextState)
        {
            if (Comparer.Equals(currentStateType, nextState)) return;

            CurrentState?.Exit((TController)this, nextState);

            currentStateType = nextState;

            // 캐싱된 매니저에게 상태 객체를 요청 (new 할당 없음)
            CurrentState = _stateManager.GetState(nextState);

            CurrentState?.Enter((TController)this);
        }
    }
}