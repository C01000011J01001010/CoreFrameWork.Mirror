using System;

namespace CoreEngine.DesignPattern.StateMachine
{
    public abstract class BaseState<TState, TController> : IState<TState, TController>
        where TState : struct, Enum
        where TController : class
    {
        public abstract void Enter(TController controller);
        public abstract TState? CheckTransitions(TController controller);

        // Update류는 필수 구현이 아닐 수 있으므로 virtual로 둡니다.
        public virtual void Update(TController controller, float deltaTime) { }
        public virtual void FixedUpdate(TController controller, float fixedDeltaTime) { }

        public abstract void Exit(TController controller, TState? nextState);
    }
}