using CoreEngine.Manager.Pool;
using System;
using UnityEngine;

namespace CoreEngine.Manager
{
    /// <summary>
    /// 로컬 풀링 시스템을 관리하는 매니저
    /// </summary>
    public abstract class BaseObjectPoolManager<TPoolType> : BasePoolManager<TPoolType, ObjectPoolHandler<TPoolType>>
        where TPoolType : Enum
    {
        protected override void OnValidate()
        {
            base.OnValidate();
            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                if (!setup.prefab.TryGetComponent(out IPoolable _))
                {
                    Debug.LogError($"[{setup.prefab.name}]은(는) {nameof(BaseObjectPoolManager<TPoolType>)}의 조건을 만족하지 않습니다.\n({nameof(IPoolable)} 필요)");
                    setup.prefab = null;
                }
            }
        }
    }
}
