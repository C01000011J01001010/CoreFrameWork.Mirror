using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CoreEngine.Manager.Pool
{
    public interface IPoolReleaser
    {
        void Release(IPoolable pObj);
    }
    /// <summary>
    /// 순수 C#으로 분리된 풀링 논리 처리기 (부품)
    /// </summary>
    public abstract class BasePoolHandler<TPoolType> : IPoolReleaser
        where TPoolType : Enum
    {
        protected PoolSetup<TPoolType> _setup;
        protected Transform _parent;
        protected Func<bool> _isShuttingDown; // Host로부터 씬 종료 상태를 묻는 델리게이트
        protected IObjectPool<IPoolable> _pool;

        private bool _isInit = false;
        public void Initialize (PoolSetup<TPoolType> setup, Transform parent, Func<bool> isShuttingDown)
        {
            if (_isInit) return;

            _setup = setup;
            _parent = parent;
            _isShuttingDown = isShuttingDown;

            _pool = new ObjectPool<IPoolable>(
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

            _isInit = true;
        }

        #region Pool Callbacks

        protected IPoolable CreateItem()
        {
            GameObject obj = UnityEngine.Object.Instantiate(_setup.prefab.gameObject, _parent);
            if (obj.TryGetComponent(out IPoolable pObj))
            {
                pObj.Releaser = this;
                return pObj;
            }
            return null;
        }

        protected void OnTakeFromPool(IPoolable pObj)
        {
            pObj.gameObject.SetActive(true);
            pObj.OnSpawn();
        }

        protected void OnReturnedToPool(IPoolable pObj)
        {
            if (_isShuttingDown() || pObj == null || pObj.gameObject == null) return;

            // 비활성화 및 풀 반환 전 상태 초기화
            pObj.OnDespawn();
            pObj.gameObject.SetActive(false);
            pObj.transform.SetParent(_parent);
        }

        protected void OnDestroyPoolObject(IPoolable pObj)
        {
            if (pObj != null && pObj.gameObject != null)
                UnityEngine.Object.Destroy(pObj.gameObject);
        }

        #endregion

        #region 외부 API
        public virtual IPoolable Spawn(Vector3 position)
        {
            IPoolable pObj = _pool.Get();
            if (pObj != null && pObj.transform != null)
                pObj.transform.position = position;

            return pObj;
        }

        // Host의 코루틴에서 호출될 1스텝 프리워밍 로직
        public void PrewarmStep(List<IPoolable> prewarmCache)
        {
            prewarmCache.Add(_pool.Get());
        }

        public void ReturnPrewarm(List<IPoolable> prewarmCache)
        {
            foreach (var pObj in prewarmCache)
            {
                _pool.Release(pObj);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public virtual void Release(IPoolable pObj)
        {
            _pool.Release(pObj);
        }
        #endregion



    }
}