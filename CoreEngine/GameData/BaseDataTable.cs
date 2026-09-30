using CoreEngine.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.GameData
{
    /// <summary>
    /// csv 컨버터에서 에디터 전용으로 접근할 완벽한 리모컨(인터페이스)
    /// </summary>
    internal interface IDataTableSetter : IPreloadAssetIdsBaker
    {
#if UNITY_EDITOR
        void SetCapacity(int count);
        bool Add(IDataRecord record);
        void Clear();
        List<IDataRecord> GetListCopy();
#endif
    }

    public abstract class BaseDataTable<TRecord> : _DataTable, IDataTableSetter
        where TRecord : class, IDataRecord, new()
    {
        [SerializeField, ReadOnly]
        private List<TRecord> _table = new();

        private Dictionary<int, TRecord> _tableDict = null;

        internal Dictionary<int, TRecord> GetCachedTableDict()
        {
            if (_tableDict != null) return _tableDict;

            CacheTableDict();
            return _tableDict;
        }

        private void CacheTableDict()
        {
            if (_table.Count == 0)
            {
                LogHelper.LogWarning($"table({this.name})이 사용할 수 없는 상태입니다.");
                return;
            }

            _tableDict = new(_table.Count);
            for (int i = 0; i < _table.Count; i++)
            {
                int index = _table[i].Id;
                if (_tableDict.ContainsKey(index))
                {
                    LogHelper.LogWarning($"{this.name}에 동일한 index의 데이터가 존재합니다.");
                    continue;
                }
                _tableDict.Add(index, _table[i]);
            }
        }

#if UNITY_EDITOR
        private static readonly IComparer<TRecord> _indexComparer = Comparer<TRecord>.Create((x, y) => x.Id.CompareTo(y.Id));

        void IDataTableSetter.SetCapacity(int count)
        {
            if (count > _table.Capacity)
                _table.Capacity = count;
        }

        bool IDataTableSetter.Add(IDataRecord newRecord)
        {
            if (newRecord is not TRecord recordAsT) return false;

            int listIndex = _table.BinarySearch(recordAsT, _indexComparer);
            if (listIndex >= 0) return false;

            listIndex = ~listIndex;
            _table.Insert(listIndex, recordAsT);
            _tableDict = null;
            return true;
        }

        void IDataTableSetter.Clear()
        {
            _table.Clear();
            _tableDict = null;
        }

        List<IDataRecord> IDataTableSetter.GetListCopy()
        {
            return new List<IDataRecord>(_table);
        }
#endif
    }
}