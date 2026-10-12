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
        public delegate ulong GetForeignKeyHash(TRecord record);

        [SerializeField]
        private TRecord[] _table;


        #region 런타임 캐시

        private Dictionary<ulong/*내 레코드 해시*/, TRecord> _runtimeRecordDict;

        private Dictionary<uint/*index*/, 
            Dictionary<ulong/*참조 대상의 해시*/, List<TRecord>>> _reverseIndexes;

        #endregion

        private void OnEnable()
        {
            _runtimeRecordDict = null;
            _reverseIndexes = null;
        }

        #region 초기화

        internal Dictionary<ulong, TRecord> GetCachedTableDict()
        {
            if(_runtimeRecordDict == null) InitializeRuntimeCache();
            return _runtimeRecordDict;
        }

        public override void InitializeRuntimeCache()
        {
            InitializeTableDict();
            InitializeReverseIndex();
        }
        private void InitializeTableDict()
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

        private void InitializeReverseIndex()
        {
            foreach(var kv in GetReverseIndexBuilder())
            {
                BuildReverseIndex(kv.Key, kv.Value);
            }
        }

        // 필요시 재정의
        protected virtual List<KeyValuePair<uint, GetForeignKeyHash>> GetReverseIndexBuilder()
        {
            return new();
        }

        private void BuildReverseIndex(uint index, GetForeignKeyHash getForignKey)
        {
            if (_reverseIndexes == null) _reverseIndexes = new();

            Dictionary<ulong, List<TRecord>> indexMap = new();

            foreach (var record in _table)
            {
                ulong fk = getForignKey(record);

                if (fk == 0) continue; // 0이거나 유효하지 않은 키는 건너뜀

                if (!indexMap.ContainsKey(fk)) 
                {
                    indexMap[fk] = new List<TRecord>();
                }
                indexMap[fk].Add(record);
            }

            _reverseIndexes[index] = indexMap;
        }

        #endregion

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

        /// <summary>
        /// 타겟 ID를 나를 참조하고 있는 레코드 리스트를 반환
        /// </summary>
        public List<TRecord> GetReferencingRecords(uint index, ulong targetId)
        {
            if(_reverseIndexes == null)
            {
                LogHelper.Log($"{GetType().Name}은 참조키 인덱스 Dictionary가 존재하지 않습니다.");
                return null;
            }

            if(!_reverseIndexes.TryGetValue(index, out var indexMap))
            {
                LogHelper.LogWarning($"{GetType().Name}의 참조키 인덱스로 \"{index}\"가 유효하지 않습니다.");
                return null;
            }

            if (!indexMap.TryGetValue(targetId, out var resultList))
            {
                LogHelper.LogWarning($"{GetType()}의 참조키 인덱스 \"{index}\"에서 id \"{targetId}\"를 참조하지 않습니다.");
                return null;
            }

            return resultList;
        }

#if UNITY_EDITOR

        bool ITableSetter.Set(List<IRecord> newRecords)
        {
            _table = newRecords.Cast<TRecord>().ToArray();
            return true;
        }

        void ITableSetter.Clear()
        {
            _table = null;
            _runtimeRecordDict = null;
            _reverseIndexes = null;
        }
#endif
    }
}