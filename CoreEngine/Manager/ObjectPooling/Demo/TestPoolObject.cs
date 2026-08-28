using CoreEngine;
using System;
using UnityEngine;
using UnityEngine.Pool;

namespace CoreEngine.Pool.Test
{
    public class TestPoolObject : MonoBehaviour, IPoolable
    {
        public IObjectPool<GameObject> RootPool { get; set; }

        public void OnDespawn()
        {
            UtilityLog.Log($"[TestPoolObject] OnDespawn() called for {gameObject.name}");
        }

        public void OnSpawn()
        {
            UtilityLog.Log($"[TestPoolObject] OnSpawn() called for {gameObject.name}");
        }
    }
}
