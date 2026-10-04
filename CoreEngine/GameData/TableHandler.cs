using CoreEngine.Helpers;
using System.Collections.Generic;

namespace CoreEngine.GameData
{
    public class TableHandler<TTable, TRecord>
        where TTable : BaseTable<TRecord>
        where TRecord : class, IRecord
    {
        private TTable _table;
        private TTable Table => _table ??= TableRouter.GetTable<TTable>();
        private Dictionary<int, TRecord> _recordDict;
        private Dictionary<int, TRecord> RecordDict => _recordDict ??= Table?.GetCachedTableDict();

        public TRecord GetRecord(int id)
        {
            if (RecordDict == null)
            {
                LogHelper.LogWarning($"{GetType().Name}.{nameof(GetRecord)} Failed. Table is null. ID: {id}");
                return null;
            }
            if (!_recordDict.TryGetValue(id, out TRecord record))
            {
                LogHelper.LogWarning($"{GetType().Name}.{nameof(GetRecord)} Failed. Table has no record. ID: {id}");
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
            if (RecordDict == null)
            {
                LogHelper.LogWarning($"{GetType().Name}.{nameof(GetRecords)} Failed. Table is null.");
                return result;
            }

            foreach (var record in _recordDict.Values)
            {
                if (predicate(record)) result.Add(record);
            }
            return result;
        }
    }
}