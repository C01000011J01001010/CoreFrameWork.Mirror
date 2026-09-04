using CoreEngine.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine
{
    public abstract class BaseStateManager<TState, TController> : BaseManager
        where TState : struct, Enum
        where TController : class
    {
        private readonly Dictionary<TState, IState<TState, TController>> stateDict = new();

        protected override void Awake()
        {
            base.Awake();
            SetUpStates();
            ValidateStates();
        }

        protected abstract void SetUpStates();

        // 자식 클래스에서 상태를 등록할 때 사용할 안전한 메서드
        protected void AddState(TState key, IState<TState, TController> state)
        {
            // TryAdd는 중복 키가 있으면 false를 반환하고 Exception을 내지 않음
            if (!stateDict.TryAdd(key, state))
            {
                LogHelper.Log($"[StateManager] State '{key}' is already registered! Ignoring duplicate.", LogColor.Yellow);
            }
        }

        public IState<TState, TController> GetState(TState wantState)
        {
            if (stateDict.TryGetValue(wantState, out var state))
            {
                return state;
            }
            LogHelper.Log($"[StateManager] The key({wantState}) not contained in stateDict!", LogColor.Red);
            return null;
        }

        // 무결성 검증
        [Conditional("UNITY_EDITOR")]
        private void ValidateStates()
        {

            // TState(Enum)에 정의된 모든 값을 순회하며 딕셔너리에 있는지 확인
            foreach (TState stateKey in Enum.GetValues(typeof(TState)))
            {
                if (!stateDict.ContainsKey(stateKey))
                {
                    LogHelper.Log($"[StateManager] Critical Warning: State '{stateKey}' is defined in Enum but not registered in InitializeStates()!", LogColor.Red);
                }
            }

        }
    }
}