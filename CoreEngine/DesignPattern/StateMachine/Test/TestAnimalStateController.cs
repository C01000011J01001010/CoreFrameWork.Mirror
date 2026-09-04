using System;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine.Test
{
    // 이동 관련 데이터 묶음
    [Serializable]
    public class MovementData
    {
        public float moveSpeed = 3f;
        public float wanderRadius = 5f;
        public Vector3 currentDestination;
    }

    // 생존/스탯 관련 데이터 묶음
    [Serializable]
    public class StatData
    {
        public float hunger = 50f;
        public float maxHunger = 100f;
        public float hungerDecreaseRate = 30f; // 밥 먹을 때 감소량
        public float hungerIncreaseRate = 5f;  // 가만히 있을 때 증가량
    }

    // 상태 머신 공용 데이터 (Blackboard 본체)
    [Serializable]
    public class AnimalBlackboard
    {
        [Header("Grouped Data")]
        public MovementData movement = new MovementData();
        public StatData stats = new StatData();

        [Header("Shared State Variables")]
        public float stateTimer = 0f; // 여러 상태가 돌려쓰는 공용 타이머
        public float idleDuration = 3f;
    }

    // Controller 구현체 (데이터와 흐름 제어 담당)
    [Serializable]
    public class TestAnimalStateController : BaseStateController<TestAnimalStateKey, TestAnimalStateManager, TestAnimalStateController>
    {
        [SerializeField]
        public AnimalBlackboard _animalBlackboard = new();
        public AnimalBlackboard AnimalBlackboard = new();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // 초기 상태 지정
            defaultStateType = TestAnimalStateKey.Idle;
        }
    }
}