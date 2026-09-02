using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine.Manager.Pool
{
    public class ObjectPoolHandler<TPoolType> : BasePoolHandler<TPoolType>
        where TPoolType : Enum
    {
        public override void Release(GameObject obj)
        {
            if (obj == null) return;
            base.Release(obj);
        }
    }
}
