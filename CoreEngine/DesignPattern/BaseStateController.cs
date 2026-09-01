using CoreEngine.Actor;
using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine
{
    public abstract class BaseStateController<TState> : BaseActorFeature where TState : struct, Enum
    {
        [SerializeField] protected TState defaultStateType;
        protected TState currentStateType;
        protected BaseState<TState> CurrentState;
        protected Dictionary<TState, BaseState<TState>> stateDict;

        // GC(가비지 컬렉션) 발생을 원천 차단하기 위한 정적 캐싱
        private static readonly EqualityComparer<TState> Comparer = EqualityComparer<TState>.Default;

        public TState CurrentStateType => currentStateType;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            stateDict = ProductState();
        }

        protected abstract Dictionary<TState, BaseState<TState>> ProductState();

        // IActorHost의 OnSpawn() 시점에 호출되어야 함
        public void StartState()
        {
            currentStateType = defaultStateType;
            CurrentState = GetState(defaultStateType);
            CurrentState?.Enter();
        }

        // IActorHost의 OnDespawn() 시점에 호출되어 찌꺼기 상태 초기화
        public virtual void StopState()
        {
            CurrentState?.Exit(null);
            CurrentState = null;
        }

        public virtual void Tick(float deltaTime)
        {
            if (CurrentState == null) return;

            TState? nextState = CurrentState.CheckTransitions();

            if (nextState.HasValue)
            {
                TransitionTo(nextState.Value);
                return;
            }
            CurrentState.Update(deltaTime);
        }

        public virtual void FixedTick(float FixedDeltaTime)
        {
            CurrentState?.FixedUpdate(FixedDeltaTime);
        }

        protected virtual void TransitionTo(TState nextState)
        {
            // Boxing 없이 Enum을 비교하여 GC 오버헤드 0 달성
            if (Comparer.Equals(currentStateType, nextState)) return;

            CurrentState?.Exit(nextState);

            currentStateType = nextState;
            CurrentState = GetState(nextState);

            CurrentState?.Enter();
        }

        protected virtual BaseState<TState> GetState(TState wantState)
        {
            if (stateDict.TryGetValue(wantState, out var state))
            {
                return state;
            }
            LogHelper.Log($"the key({wantState}) not contained in stateDict", LogColor.Red);
            return null;
        }
    }
}