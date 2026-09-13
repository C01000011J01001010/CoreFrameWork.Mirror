using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.FilePathAttribute;

namespace CoreEngine.Pool
{
    public interface IPoolReleaser
    {
        void Release(IPoolable pObj);
    }
    /// <summary>
    /// 순수 C#으로 분리된 풀링 논리 처리기 (부품)
    /// </summary>
    public class ObjectPoolHandler<TPoolType> : IPoolReleaser
        where TPoolType : Enum
    {
        protected PoolSetup<TPoolType> _setup;
        protected Transform _parent;
        protected Func<bool> _isShuttingDown; // Host로부터 씬 종료 상태를 묻는 델리게이트
        protected IObjectPool<IPoolable> _pool;

        public int TotalAllocatedCount
        {
            get
            {
                // IObjectPool을 구체 클래스인 ObjectPool로 캐스팅하여 CountAll 접근
                // Spawn된 객체와 Pool에 있는 객체의 개수 합
                return _pool is ObjectPool<IPoolable> concretePool ? concretePool.CountAll : 0;
            }
        }

        private bool _isInit = false;
        public virtual void Initialize (PoolSetup<TPoolType> setup, Transform parent, Func<bool> isShuttingDown)
        {
            if (_isInit) return;

            _setup = setup;
            _parent = parent;
            _isShuttingDown = isShuttingDown;

            _pool = new ObjectPool<IPoolable>(
                createFunc: Create,
                actionOnGet: null,
                actionOnRelease: null,//OnReturnedToPool,
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

        protected IPoolable Create()
        {
            GameObject obj = UnityEngine.Object.Instantiate(_setup.prefab.gameObject, _parent);
            obj.SetActive(false);
            if (obj.TryGetComponent(out IPoolable pObj))
            {
                pObj.Releaser = this;
                return pObj;
            }
            return null;
        }

        //protected void OnReturnedToPool(IPoolable pObj)
        //{
        //    if (_isShuttingDown() || pObj == null || pObj.gameObject == null) return;

        //    // 비활성화 및 풀 반환 전 상태 초기화
        //    pObj.gameObject.SetActive(false);
        //    pObj.transform.SetParent(_parent);
        //}

        protected void OnDestroyPoolObject(IPoolable pObj)
        {
            if (pObj != null && pObj.gameObject != null)
                UnityEngine.Object.Destroy(pObj.gameObject);
        }

        #endregion

        #region 외부 API

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


        /// <summary>
        /// 스폰 이후 position과 rotation을 코드로 바꾸지 않도록 명시함
        /// </summary>
        public virtual IPoolable Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            IPoolable pObj = _pool.Get();
            if (pObj != null && pObj.gameObject != null)
            {
                pObj.transform.SetParent(parent); // null이면 root로 이동
                pObj.transform.position = position;
                pObj.transform.rotation = rotation;

                // 네트워크 객체의 경우 active 이후 위치와 회전을 바꾸면 무시 될 수 있으니
                // 먼저 Transform을 적용 후 active함
                pObj.gameObject.SetActive(true);
                pObj.OnSpawn();
            }
            return pObj;
        }

        public virtual void Release(IPoolable pObj)
        {
            // 제대로 집어넣기
            if (_isShuttingDown() || pObj == null || pObj.gameObject == null) return;

            // 비활성화 및 풀 반환 전 상태 초기화
            pObj.gameObject.SetActive(false);
            pObj.transform.SetParent(_parent);
            pObj.OnDespawn();
            _pool.Release(pObj);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        #endregion
    }
}