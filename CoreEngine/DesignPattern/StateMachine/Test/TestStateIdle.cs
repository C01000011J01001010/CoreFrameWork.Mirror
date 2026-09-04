using CoreEngine.DesignPattern.StateMachine;
using CoreEngine.Helpers;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine.Test
{
    public class TestStateIdle : BaseState<TestAnimalStateKey, TestAnimalStateController>
    {
        public override void Enter(TestAnimalStateController controller)
        {
            LogHelper.Log("동물이 가만히 서 있습니다. (Idle Enter)");
            controller.AnimalBlackboard.stateTimer = 0f;
        }

        public override TestAnimalStateKey? CheckTransitions(TestAnimalStateController controller)
        {
            var board = controller.AnimalBlackboard;

            if (board.stats.hunger >= board.stats.maxHunger)
            {
                return TestAnimalStateKey.Eat;
            }

            if (board.stateTimer >= board.idleDuration)
            {
                return TestAnimalStateKey.Wander;
            }

            return null;
        }

        public override void Update(TestAnimalStateController controller, float deltaTime)
        {
            var board = controller.AnimalBlackboard;

            board.stateTimer += deltaTime;
            board.stats.hunger += deltaTime * board.stats.hungerIncreaseRate;
        }

        public override void Exit(TestAnimalStateController controller, TestAnimalStateKey? nextState)
        {
            LogHelper.Log($"동물이 대기를 끝냅니다. 다음 상태: {nextState}");
        }
    }

    public class TestStateWander : BaseState<TestAnimalStateKey, TestAnimalStateController>
    {
        public override void Enter(TestAnimalStateController controller)
        {
            LogHelper.Log("동물이 맵을 돌아다니기 시작합니다. (Wander Enter)", LogColor.Cyan);
            controller.AnimalBlackboard.stateTimer = 0f;
        }

        public override TestAnimalStateKey? CheckTransitions(TestAnimalStateController controller)
        {
            var board = controller.AnimalBlackboard;

            if (board.stats.hunger >= board.stats.maxHunger)
            {
                return TestAnimalStateKey.Eat;
            }

            // 5초간 배회하면 다시 대기 상태로
            if (board.stateTimer >= 5f)
            {
                return TestAnimalStateKey.Idle;
            }

            return null;
        }

        public override void Update(TestAnimalStateController controller, float deltaTime)
        {
            var board = controller.AnimalBlackboard;

            board.stateTimer += deltaTime;
            // 배회 중 배고픔 증가 속도를 Idle(5)보다 높은 임의 값(10)으로 적용
            board.stats.hunger += deltaTime * 10f;
        }

        public override void Exit(TestAnimalStateController controller, TestAnimalStateKey? nextState) { }
    }

    public class TestStateEat : BaseState<TestAnimalStateKey, TestAnimalStateController>
    {
        public override void Enter(TestAnimalStateController controller)
        {
            LogHelper.Log("동물이 먹이를 먹기 시작합니다. (Eat Enter)", LogColor.Green);
        }

        public override TestAnimalStateKey? CheckTransitions(TestAnimalStateController controller)
        {
            var board = controller.AnimalBlackboard;

            // 배부름에 도달하면 대기 상태로 복귀
            if (board.stats.hunger <= 0f)
            {
                return TestAnimalStateKey.Idle;
            }

            return null;
        }

        public override void Update(TestAnimalStateController controller, float deltaTime)
        {
            var stats = controller.AnimalBlackboard.stats;

            stats.hunger -= deltaTime * stats.hungerDecreaseRate;
            if (stats.hunger < 0f) stats.hunger = 0f;
        }

        public override void Exit(TestAnimalStateController controller, TestAnimalStateKey? nextState)
        {
            LogHelper.Log("식사를 마쳤습니다.", LogColor.Green);
        }
    }
}