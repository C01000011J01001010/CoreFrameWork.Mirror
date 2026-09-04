using CoreEngine.DesignPattern.StateMachine;
using CoreEngine.Helpers;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine.Test
{
    // 1. 상태를 정의할 Enum
    public enum TestAnimalStateKey
    {
        Idle,
        Wander,
        Eat
    }
}