using System;
using UnityEngine;

namespace CoreEngine.Manager.Pool
{
    [Serializable]
    public class PoolSetup<TPoolType> where TPoolType : Enum
    {
        public TPoolType poolType;
        public GameObject prefab;

        private const int maxCount = 256;
        [Range(1, maxCount)] public int defaultAmount = 8;
        [Range(2, maxCount)] public int defaultCapacity = 16;
        [Range(2, maxCount)] public int maxSize = 128;

        public PoolSetup() { SetDefaultValues(); }

        public void SetDefaultValues()
        {
            defaultAmount = 8;
            defaultCapacity = 16;
            maxSize = 128;
        }

#if UNITY_EDITOR
        public void ValidateValues()
        {
            if (defaultAmount < 1 || defaultCapacity < 2 || maxSize < 2)
            {
                SetDefaultValues();
                return;
            }
            if (defaultAmount > defaultCapacity) defaultCapacity = defaultAmount;
            if (defaultCapacity > maxSize) maxSize = defaultCapacity;
        }
#endif
    }
}