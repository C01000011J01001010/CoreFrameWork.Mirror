using UnityEngine;
using CoreEngine.Helpers;

namespace CoreEngine.Manager.Pool.Test
{
    public class TestPoolObject : MonoBehaviour, IPoolable
    {
        public IPoolReleaser Releaser { get; set; }

        public void OnDespawn()
        {
            LogHelper.Log($"[TestPoolObject] OnDespawn() called for {gameObject.name}");
        }

        public void OnSpawn()
        {
            LogHelper.Log($"[TestPoolObject] OnSpawn() called for {gameObject.name}");
        }
    }
}
