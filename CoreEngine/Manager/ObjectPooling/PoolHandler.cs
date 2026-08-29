using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CoreEngine.Manager.Pool
{
    /// <summary>
    /// 순수 C#으로 분리된 풀링 논리 처리기 (부품)
    /// </summary>
    public class PoolHandler<TPoolType> where TPoolType : Enum
    {
        private readonly PoolSetup<TPoolType> _setup;
        private readonly Transform _parent;
        private readonly Func<bool> _isShuttingDown; // Host로부터 씬 종료 상태를 묻는 델리게이트
        private readonly IObjectPool<GameObject> _pool;

        public PoolHandler(PoolSetup<TPoolType> setup, Transform parent, Func<bool> isShuttingDown)
        {
            _setup = setup;
            _parent = parent;
            _isShuttingDown = isShuttingDown;

            _pool = new ObjectPool<GameObject>(
                createFunc: CreateItem,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
#if UNITY_EDITOR
                collectionCheck: true,
#else
                collectionCheck: false,
#endif
                defaultCapacity: _setup.defaultCapacity,
                maxSize: _setup.maxSize
            );
        }

        #region Pool Callbacks

        private GameObject CreateItem()
        {
            GameObject obj = UnityEngine.Object.Instantiate(_setup.prefab, _parent);
            if (obj.TryGetComponent(out IPoolable poolableItem))
            {
                poolableItem.RootPool = _pool;
            }
            return obj;
        }

        private void OnTakeFromPool(GameObject obj)
        {
            obj.SetActive(true);
            if (obj.TryGetComponent(out IPoolable poolableItem))
            {
                poolableItem.OnSpawn();
            }
        }

        private void OnReturnedToPool(GameObject obj)
        {
            if (_isShuttingDown() || obj == null) return;

            // 비활성화 및 풀 반환 전 상태 초기화
            if (obj.TryGetComponent(out IPoolable poolableItem))
            {
                poolableItem.OnDespawn();
            }

            obj.SetActive(false);
            obj.transform.SetParent(_parent);
        }

        private void OnDestroyPoolObject(GameObject obj)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        #endregion

        #region PoolManager에 호출 위임
        public GameObject Spawn(Vector3 position)
        {
            GameObject obj = _pool.Get();
            if (obj != null) obj.transform.position = position;
            return obj;
        }

        // Host의 코루틴에서 호출될 1스텝 프리워밍 로직
        public void PrewarmStep(List<GameObject> prewarmCache)
        {
            prewarmCache.Add(_pool.Get());
        }

        public void ReturnPrewarm(List<GameObject> prewarmCache)
        {
            foreach (var obj in prewarmCache)
            {
                _pool.Release(obj);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }
        #endregion
    }
}