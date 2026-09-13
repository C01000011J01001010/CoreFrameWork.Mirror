using System;
using UnityEngine;

namespace CoreEngine.Pool
{
    /// <summary>
    /// 로컬 풀링 시스템을 관리하는 매니저
    /// </summary>
    public abstract class BaseObjectPoolManager<TPoolType> : BasePoolManager<TPoolType, ObjectPoolHandler<TPoolType>>
        where TPoolType : Enum
    {

    }
}
