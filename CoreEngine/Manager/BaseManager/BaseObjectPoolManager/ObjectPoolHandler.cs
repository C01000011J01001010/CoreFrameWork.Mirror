using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine.Manager.Pool
{
    public class ObjectPoolHandler<TPoolType> : BasePoolHandler<TPoolType>
        where TPoolType : Enum
    {
        public override IPoolable Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            IPoolable pObj = base.Spawn(position, rotation, parent);
            pObj.OnSpawn(); // 로컬에서는 즉시 OnSpawn 처리
            return pObj;
        }
        public override void Release(IPoolable pObj)
        {
            if (pObj == null) return;
            base.Release(pObj);
        }
    }
}
