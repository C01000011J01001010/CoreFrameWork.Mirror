using UnityEngine;
using CoreEngine.Helpers;

namespace CoreEngine.Pool.Test
{
    public class TestPoolObject : MonoBehaviour, IPoolable
    {
        public IPoolReleaser Releaser { get; set; }

        int id;

        private void Start()
        {
            id = gameObject.GetInstanceID();
        }

        public void OnSpawn()
        {
            LogHelper.Log($"({gameObject.name}.{id}) 등장", LogColor.Green);
            TestPoolTracker.SpawnedObjects.Add(this);
        }

        public void OnDespawn()
        {
            if (TestPoolTracker.SpawnedObjects.Remove(this))
            {
                LogHelper.Log($"({gameObject.name}.{id}) 퇴장", LogColor.Blue);
            }
        }

        
    }
}
