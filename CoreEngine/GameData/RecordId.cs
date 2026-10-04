using System;
using UnityEngine;

namespace CoreEngine.GameData
{
    [Serializable]
    public struct RecordId<TRecord, TTable> : IIdentifiable, IEquatable<RecordId<TRecord, TTable>>
        where TRecord : class, IRecord
        where TTable : BaseTable<TRecord>
    {
        public readonly static Type TableType = typeof(TTable);

        [SerializeField]
        private ulong id;
        ulong IIdentifiable.ID => id;

        public RecordId(ulong id)
        {
            this.id = id;
        }

        public TRecord Get()
        {
            return TableRouter.GetRecord(TableType, id) as TRecord;
        }

        public static implicit operator RecordId<TRecord, TTable>(ulong id)
            => new RecordId<TRecord, TTable>(id);

        public bool Equals(RecordId<TRecord, TTable> other)
            => id == other.id;

        public override bool Equals(object obj)
            => obj is RecordId<TRecord, TTable> other && Equals(other);

        public override int GetHashCode() => id.GetHashCode();

        public static bool operator ==(RecordId<TRecord, TTable> left, RecordId<TRecord, TTable> right)
            => left.Equals(right);

        public static bool operator !=(RecordId<TRecord, TTable> left, RecordId<TRecord, TTable> right)
            => !left.Equals(right);
    }
}
