using System;
using CoreEngine.Actor;

namespace CoreEngine.DesignPattern.StateMachine
{
    // IState는 이제 IActorHost를 통해 Host(Actor)와 상호작용합니다.
    public interface IState<TState>
        where TState : struct, Enum
    {
        void Enter(IActorHost host);
        TState? CheckTransitions(IActorHost host);
        void Update(IActorHost host, float deltaTime);
        void FixedUpdate(IActorHost host, float fixedDeltaTime);
        void Exit(IActorHost host, TState? nextState);
    }
}
