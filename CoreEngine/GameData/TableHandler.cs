using CoreEngine.Helpers;
using System.Collections.Generic;

namespace CoreEngine.GameData
{
    public class TableHandler<TTable, TRecord>
        where TTable : BaseTable<TRecord>
        where TRecord : class, IRecord
    {
        private TTable _table;
        private TTable Table => _table ??= TableRouter.GetTable<TTable>(); // TableRouter 가정

        private Dictionary<ulong, TRecord> _recordDict;
        private Dictionary<ulong, TRecord> RecordDict => _recordDict ??= Table?.GetCachedTableDict();

        // ==========================================
        // 1. 코어 룩업 (ulong 기반 - 제로 오버헤드)
        // ==========================================
        public TRecord GetRecord(ulong HashCodeKey)
        {
            if (RecordDict == null)
            {
                LogHelper.LogWarning($"{GetType().Name}.{nameof(GetRecord)} Failed. Table is null. ID: {HashCodeKey}");
                return null;
            }
            if (!_recordDict.TryGetValue(HashCodeKey, out TRecord record))
            {
                LogHelper.LogWarning($"{GetType().Name}.{nameof(GetRecord)} Failed. Table has no record. ID: {HashCodeKey}");
                return null;
            }
            return record;
        }

        public bool TryGetRecord(ulong HashCodeKey, out TRecord record)
        {
            record = GetRecord(HashCodeKey);
            return record is not null;
        }

        // ==========================================
        // 2. 편의성 룩업 (string 기반 - 딜레마 해결!)
        // ==========================================
        public TRecord GetRecord(string stringKey)
        {
            // 브릿지(공통 공식)를 통해 string을 ulong으로 변환한 뒤, 코어 룩업 호출
            ulong id = HashHelper.StringToId(stringKey);
            return GetRecord(id);
        }

        public bool TryGetRecord(string stringKey, out TRecord record)
        {
            ulong id = HashHelper.StringToId(stringKey);
            return TryGetRecord(id, out record);
        }

        // ==========================================
        // 3. 필터링 검색
        // ==========================================
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