using System;
using UnityEngine;

namespace CoreEngine.Manager.Pool
{
    public interface IPoolable
    {
        /// <summary>
        /// 풀링 객체가 돌아갈 곳을 정해주는 객체 (PoolHandler)
        /// </summary>
        public IPoolReleaser Releaser { get; set; }

        public void OnSpawn();
        public void OnDespawn();

        // 캐싱 시간 또는 GetComponent 시간을 줄이기 위해 PoolHandler에서 GameObject가 아닌 인터페이스로 관리
        // gameObject와 transform은 Monobehaviour에서 제공하는 속성이므로
        // IPoolable에서도 제공하여 PoolHandler가 직접 접근 가능하도록 함 (상세 클래스에서 구현 필요없음)
        #region Monobehaviour-like properties and methods
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public bool TryGetComponent<T>(out T component);
        #endregion
    }
}
    