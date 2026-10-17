using CoreEngine.Helpers;
using System.Collections;
using UnityEngine;

namespace CoreEngine.GameData
{
    public interface IIdentifiable { ulong ID { get; } }
    public interface IRecord : IIdentifiable 
    {
        ulong HashedPK { get; }
    }

    /// <summary>
    /// <para>규칙1: 데이터 필드는 protected로 선언하며, 이름은 소문자로 시작한다.</para>
    /// <para>규칙2: 데이터 필드에 대한 접근 프로퍼티는 public으로 선언하며, 이름은 대문자로 시작한다.</para>
    /// </summary>
    [System.Serializable]
    public abstract partial class BaseRecord: IRecord
    {
        [SerializeField]
        private ulong _hash;

        public ulong HashedPK => _hash;
        ulong IIdentifiable.ID => _hash;
    }
}

