using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine
{
    public interface IPoolable
    {
        /// <summary>
        /// poolManager의 Queue
        /// </summary>
        public Queue<GameObject> RootQueue { get; set; }
        public void OnSpawn();
        public void OnDespawn();
    }
}
    