using CoreEngine.Helpers;
using System;
using UnityEngine;

namespace CoreEngine.GameData
{
    [Serializable]
    public struct ForeignKey<TRecord, TTable> : IIdentifiable, IEquatable<ForeignKey<TRecord, TTable>>
#if UNITY_EDITOR
        , IReferenceKey
#endif
        where TRecord : class, IRecord
        where TTable : BaseTable<TRecord>
    {
        public readonly static Type TableType = typeof(TTable);

#if UNITY_EDITOR
        [SerializeField]
        private string _key;
#endif

        [SerializeField]
        private ulong _hash;

        public ulong TargetHashedPK => _hash;
        ulong IIdentifiable.ID => _hash;


        public ForeignKey(string key)
        {
#if UNITY_EDITOR
            _key = key;
#endif
            _hash = HashHelper.StringToId(key);
        }

        public TRecord Get()
        {
            return TableRouter.GetRecord(TableType, _hash) as TRecord;
        }


        public bool Equals(ForeignKey<TRecord, TTable> other)
            => _hash == other._hash;

        public override bool Equals(object obj)
            => obj is ForeignKey<TRecord, TTable> other && Equals(other);

        public override int GetHashCode() => _hash.GetHashCode();

        public static bool operator ==(ForeignKey<TRecord, TTable> left, ForeignKey<TRecord, TTable> right)
            => left.Equals(right);

        public static bool operator !=(ForeignKey<TRecord, TTable> left, ForeignKey<TRecord, TTable> right)
            => !left.Equals(right);

#if UNITY_EDITOR
        string IReferenceKey.GetEditorRawKey()
        {
            return _key;
        }
#endif
    }
}