using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Manager.Pool
{
    /// <summary>
    /// BaseObjectPoolManager 또는 NetworkObjectPoolManager 사용 권장
    /// </summary>
    public abstract class BasePoolManager<TPoolType, TPoolHandlerType> : BaseManager
        where TPoolType : Enum
        where TPoolHandlerType : BasePoolHandler<TPoolType>, new()
    {
        public List<PoolSetup<TPoolType>> poolSetups = new();

        // C# 부품들을 담아두는 딕셔너리
        private Dictionary<TPoolType, TPoolHandlerType> _handlers = new();

        // 씬 종료 플래그
        private bool _isShuttingDown = false;

        //protected abstract TPoolHandlerType GenerateHandler(PoolSetup<TPoolType> setup, Transform parent, Func<bool> isShuttingDown);

        public override IEnumerator Initialize()
        {
            yield return base.Initialize();

            _isShuttingDown = false;
            InitializeHandlers();

            // 2. 프리워밍 코루틴 실행 (화면 스파이크 방지)
            yield return PreWarmingRoutine();

            Debug.Log($"[{this.GetType().Name}] 풀링 매니저 초기화 및 프리워밍 완료");
        }

        public override void Exit()
        {
            // 씬 전환 시 널 레퍼런스(가짜 널) 대참사 방지용 차단벽 가동
            _isShuttingDown = true;

            foreach (var handler in _handlers.Values)
            {
                handler.Clear();
            }
            _handlers.Clear();

            base.Exit();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _isShuttingDown = true;
        }

        #region Initialize & Prewarm
        private void InitializeHandlers()
        {
            _handlers.Clear();

            foreach (var setup in poolSetups)
            {
                if (_handlers.ContainsKey(setup.poolType)) continue;
                if (setup.prefab == null) continue;

                // 유니티 Transform 자원 생성
                GameObject parentObj = new GameObject($"[{setup.poolType}_Pool]");
                parentObj.transform.SetParent(this.transform);

                // C# 부품 생성 및 주입 (이때 씬 종료 여부를 묻는 델리게이트 전달)
                TPoolHandlerType handler = new();
                handler.Initialize(setup, parentObj.transform, () => _isShuttingDown);
                _handlers.Add(setup.poolType, handler);
            }
        }

        private IEnumerator PreWarmingRoutine()
        {
            int lastTime = Environment.TickCount;

            foreach (var setup in poolSetups)
            {
                if (!_handlers.TryGetValue(setup.poolType, out var handler)) continue;

                List<IPoolable> prewarmCache = new List<IPoolable>(setup.defaultAmount);

                for (int i = 0; i < setup.defaultAmount; i++)
                {
                    handler.PrewarmStep(prewarmCache);

                    // 1프레임 내 동기화 오버헤드를 막기 위한 시간 제어
                    if (Environment.TickCount - lastTime > 100)
                    {
                        yield return null;
                        lastTime = Environment.TickCount;
                    }
                }

                handler.ReturnPrewarm(prewarmCache);
                yield return null;
            }
        }
        #endregion

        #region 외부 노출 API : 실제 연산은 Handler에 위임

        public IPoolable Spawn(TPoolType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_handlers.TryGetValue(type, out var handler)) return null;
            return handler.Spawn(position, rotation, parent);
        }

        // 2D는 rotation이 필요 없을 수 있지만 명시적 처리를 위해 열어둠
        public IPoolable Spawn2D(TPoolType type, Vector2 position2D, Quaternion rotation, Transform parent = null)
        {
            return Spawn(type, new Vector3(position2D.x, position2D.y, 0), rotation, parent);
        }
        #endregion

#if UNITY_EDITOR
        private HashSet<TPoolType> ___typeCheckSet = new HashSet<TPoolType>();

        protected override void OnValidate()
        {
            base.OnValidate();
            foreach (var setup in poolSetups)
            {
                setup.ValidateValues();

                if (setup.prefab == null)
                    Debug.LogWarning($"[{this.GetType().Name}] {setup.poolType}의 프리팹이 비어있음");

                if (!___typeCheckSet.Add(setup.poolType))
                    Debug.LogError($"[{this.GetType().Name}] 인스펙터에 {setup.poolType} 풀이 중복해서 등록되어 있음");
            }
            ___typeCheckSet.Clear();
        }
#endif
    }
}