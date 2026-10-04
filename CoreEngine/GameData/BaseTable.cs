using CoreEngine.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.GameData
{
    /// <summary>
    /// csv 컨버터에서 에디터 전용으로 접근할 완벽한 리모컨(인터페이스)
    /// </summary>
    internal interface ITableSetter : IPreloadAssetIdsBaker
    {
#if UNITY_EDITOR
        void SetCapacity(int count);
        bool Add(IRecord record);
        void Clear();
        List<IRecord> GetListCopy();
#endif
    }

    public class BaseTable<TRecord> : _Table, ITableSetter
        where TRecord : class, IRecord
    {
        [SerializeField, ReadOnly]
        private List<TRecord> _table = new();

        private Dictionary<int, TRecord> _recordDict = null;

        internal Dictionary<int, TRecord> GetCachedTableDict()
        {
            if (_recordDict != null) return _recordDict;

            InitializeRuntimeCache();
            return _recordDict;
        }

        public override void InitializeRuntimeCache()
        {
            if (_table.Count == 0)
            {
                LogHelper.LogWarning($"table({this.name})이 사용할 수 없는 상태입니다.");
                return;
            }

            _recordDict = new(_table.Count);
            for (int i = 0; i < _table.Count; i++)
            {
                int index = _table[i].Id;
                if (_recordDict.ContainsKey(index))
                {
                    LogHelper.LogWarning($"{this.name}에 동일한 index의 데이터가 존재합니다.");
                    continue;
                }
                _recordDict.Add(index, _table[i]);
            }
        }

        public override IRecord GetRecord(int id)
        {
            if (_recordDict != null && _recordDict.TryGetValue(id, out TRecord record))
            {
                return record;
            }
            LogHelper.LogWarning($"Record not found for ID: {id}");
            return null;
        }

#if UNITY_EDITOR
        private static readonly IComparer<TRecord> _indexComparer = Comparer<TRecord>.Create((x, y) => x.Id.CompareTo(y.Id));

        void ITableSetter.SetCapacity(int count)
        {
            if (count > _table.Capacity)
                _table.Capacity = count;
        }

        bool ITableSetter.Add(IRecord newRecord)
        {
            if (newRecord is not TRecord recordAsT) return false;

            int listIndex = _table.BinarySearch(recordAsT, _indexComparer);
            if (listIndex >= 0) return false;

            listIndex = ~listIndex;
            _table.Insert(listIndex, recordAsT);
            _recordDict = null;
            return true;
        }

        void ITableSetter.Clear()
        {
            _table.Clear();
            _recordDict = null;
        }

        List<IRecord> ITableSetter.GetListCopy()
        {
            return new List<IRecord>(_table);
        }

        
#endif
    }
}