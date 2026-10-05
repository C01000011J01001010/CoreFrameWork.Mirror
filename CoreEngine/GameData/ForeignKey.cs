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
        private ulong _hashcode;
        ulong IIdentifiable.ID => _hashcode;



        public ForeignKey(string key)
        {
#if UNITY_EDITOR
            _key = key;
#endif
            _hashcode = HashHelper.StringToId(key);
        }

        public TRecord Get()
        {
            return TableRouter.GetRecord(TableType, _hashcode) as TRecord;
        }

        // ===============================================
        // 비교 연산자 (유저님 작성본과 동일)
        // ===============================================
        public bool Equals(ForeignKey<TRecord, TTable> other)
            => _hashcode == other._hashcode;

        public override bool Equals(object obj)
            => obj is ForeignKey<TRecord, TTable> other && Equals(other);

        public override int GetHashCode() => _hashcode.GetHashCode();

        public static bool operator ==(ForeignKey<TRecord, TTable> left, ForeignKey<TRecord, TTable> right)
            => left.Equals(right);

        public static bool operator !=(ForeignKey<TRecord, TTable> left, ForeignKey<TRecord, TTable> right)
            => !left.Equals(right);

#if UNITY_EDITOR
        public override string ToString()
        {
            return BaseRecord.KeyException;
        }

        string IReferenceKey.GetEditorRawKey()
        {
            return _key;
        }
#endif
    }
}