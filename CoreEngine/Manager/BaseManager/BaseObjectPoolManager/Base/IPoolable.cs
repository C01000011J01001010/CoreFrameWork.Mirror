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
    }
}
    