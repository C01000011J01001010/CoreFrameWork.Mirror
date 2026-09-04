using CoreEngine.DesignPattern.StateMachine;

namespace CoreEngine.DesignPattern.StateMachine.Test
{
    // 3. Manager 구현체 (상태 객체 생성 및 캐싱 담당)
    public class TestAnimalStateManager : BaseStateManager<TestAnimalStateKey, TestAnimalStateController>
    {
        protected override void SetUpStates()
        {
            // 부모 클래스(BaseStateManager)의 AddState를 사용하여 안전하게 등록
            AddState(TestAnimalStateKey.Idle, new TestStateIdle());
            AddState(TestAnimalStateKey.Wander, new TestStateWander());
            AddState(TestAnimalStateKey.Eat, new TestStateEat());
        }
    }
}