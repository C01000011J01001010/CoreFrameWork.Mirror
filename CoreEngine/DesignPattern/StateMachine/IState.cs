using System;

namespace CoreEngine.DesignPattern.StateMachine
{
    // TController를 받아 해당 컨트롤러와 Blackboard를 조작
    public interface IState<TState, TController>
        where TState : struct, Enum
        where TController : class
    {
        void Enter(TController controller);
        TState? CheckTransitions(TController controller);
        void Update(TController controller, float deltaTime);
        void FixedUpdate(TController controller, float fixedDeltaTime);
        void Exit(TController controller, TState? nextState);
    }
}