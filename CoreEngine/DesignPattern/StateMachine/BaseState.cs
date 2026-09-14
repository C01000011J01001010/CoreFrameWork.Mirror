using System;
using CoreEngine.Actor;

namespace CoreEngine.DesignPattern.StateMachine
{
    public abstract class BaseState<TState> : IState<TState>
        where TState : struct, Enum
    {
        public abstract void Enter(IActorHost host);
        public abstract TState? CheckTransitions(IActorHost host);

        // Update류는 필수 구현이 아닐 수 있으므로 virtual로 둡니다.
        public virtual void Update(IActorHost host, float deltaTime) { }
        public virtual void FixedUpdate(IActorHost host, float fixedDeltaTime) { }

        public abstract void Exit(IActorHost host, TState? nextState);
    }
}
