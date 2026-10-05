using CoreEngine.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreEngine.GameData
{
    /// <summary>
    /// csv 컨버터에서 에디터 전용으로 접근할 완벽한 리모컨(인터페이스)
    /// </summary>
    internal interface ITableSetter : IPreloadAssetIdsBaker
    {
#if UNITY_EDITOR
        //void SetCapacity(int count);
        bool Set(List<IRecord> record);
        void Clear();
        //List<IRecord> GetListCopy();
#endif
    }

    public class BaseTable<TRecord> : _Table, ITableSetter
        where TRecord : class, IRecord
    {
        [SerializeField]
        private TRecord[] _table;

        private Dictionary<ulong, TRecord> _runtimeRecordDict;

        private void OnEnable()
        {
            _runtimeRecordDict = null;
        }

        internal Dictionary<ulong, TRecord> GetCachedTableDict()
        {
            if(_runtimeRecordDict == null) InitializeRuntimeCache();
            return _runtimeRecordDict;
        }

        public override void InitializeRuntimeCache()
        {
            if (_table.Length == 0)
            {
                LogHelper.LogWarning($"table({this.name})이 사용할 수 없는 상태입니다.");
                return;
            }

            _runtimeRecordDict = new(_table.Length);
            for (int i = 0; i < _table.Length; i++)
            {
                ulong index = _table[i].ID;
                if (_runtimeRecordDict.ContainsKey(index))
                {
                    LogHelper.LogWarning($"{this.name}에 동일한 index의 데이터가 존재합니다.");
                    continue;
                }
                _runtimeRecordDict.Add(index, _table[i]);
            }
        }

        public override IRecord GetRecord(ulong id)
        {
            if (_runtimeRecordDict == null) InitializeRuntimeCache();
            if (_runtimeRecordDict.TryGetValue(id, out TRecord record))
            {
                return record;
            }
            LogHelper.LogWarning($"Record not found for ID: {id}");
            return null;
        }

#if UNITY_EDITOR
        //void ITableSetter.SetCapacity(int count)
        //{
        //    if (count > _table.Capacity)
        //        _table.Capacity = count;
        //}

        bool ITableSetter.Set(List<IRecord> newRecords)
        {
            _table = newRecords.Cast<TRecord>().ToArray();
            //if (newRecord is not TRecord recordAsT) return false;

            //// 💡 정렬/중복검사 없이 순차 삽입 (CSV 원본 순서 유지 & O(1) 속도)
            //_table.Add(recordAsT);
            //_runtimeRecordDict = null;

            return true;
        }

        void ITableSetter.Clear()
        {
            _table = null;
            _runtimeRecordDict = null;
        }

        //List<IRecord> ITableSetter.GetListCopy()
        //{
        //    return new List<IRecord>(_table);
        //}
#endif
    }
}