using CoreEngine.Helpers;
using System.Collections.Generic;

namespace CoreEngine.GameData
{
    public class DataTableHandler<TTable, TRecord>
        where TTable : BaseTable<TRecord>
        where TRecord : class, IRecord
    {
        private readonly TTable _table;
        private readonly Dictionary<int, TRecord> _recordDict;

        public DataTableHandler()
        {
            _table = RecordRouter.GetTable<TTable>();

            if (_table == null)
            {
                LogHelper.LogWarning($"{GetType().Name}: {typeof(TTable).Name} 로드 실패.");
                return;
            }

            _recordDict = _table.GetCachedTableDict();
        }

        public TRecord GetRecord(int id)
        {
            if (_recordDict == null || !_recordDict.TryGetValue(id, out TRecord record))
            {
                LogHelper.LogWarning($"{GetType().Name}.GetRecord Failed. ID: {id}");
                return null;
            }
            return record;
        }

        public bool TryGetRecord(int id, out TRecord record)
        {
            record = GetRecord(id);
            return record != null;
        }

        // O(N) 순회 필터링
        // (추후 최적화가 필요하다면 이 내부에서 인덱싱 해시맵을 구축하도록 확장 가능)
        public List<TRecord> GetRecords(System.Func<TRecord, bool> predicate)
        {
            List<TRecord> result = new();
            if (_recordDict == null) return result;

            foreach (var record in _recordDict.Values)
            {
                if (predicate(record)) result.Add(record);
            }
            return result;
        }
    }
}