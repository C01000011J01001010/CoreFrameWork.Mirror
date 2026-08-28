using UnityEngine;
using UnityEngine.Pool;

namespace CoreEngine
{
    public interface IPoolable
    {
        /// <summary>
        /// <para>IPoolable 객체가 속한 풀의 참조를 저장하는 속성</para>
        /// <para>PoolHandler에 의해 설정됨</para> 
        /// </summary>
        public IObjectPool<GameObject> RootPool { get; set; }

        public void OnSpawn();
        public void OnDespawn();
    }
}
    