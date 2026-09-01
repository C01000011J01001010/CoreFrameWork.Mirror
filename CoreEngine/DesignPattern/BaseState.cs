using System;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine
{
    public abstract class BaseState<TState> where TState : struct, Enum
    {
        // state 시작시
        public abstract void Enter();

        // 매 프레임에서 Update보다 먼저 실행
        // 여기서 분기되면 Exit으로 이동
        public abstract TState? CheckTransitions();

        public virtual void Update(float deltaTime) { }
        public virtual void FixedUpdate(float fixedDeltaTime) { }

        // state 종료시
        public abstract void Exit(TState? nextState);
    }
}

