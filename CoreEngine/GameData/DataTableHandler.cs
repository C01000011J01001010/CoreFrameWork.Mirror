using CoreEngine.Facades;
using CoreEngine.Helpers;
using System.Collections.Generic;

namespace CoreEngine.GameData
{
    public class DataTableHandler<TTable, TRecord>
    where TTable : BaseDataTable<TRecord>
    where TRecord : class, IDataRecord, new()
    {
        private static TTable _table;
        private Dictionary<int, TRecord> _recordDict;


        /// <summary>
        /// Table을 직접 집어넣기
        /// </summary>
        public DataTableHandler(TTable table)
        {
            InternalConstructor(table);
        }

        /// <summary>
        /// 제네릭 변수를 사용하여 자동화
        /// </summary>
        public DataTableHandler()
        {
            var mag = CoreFacade.GetManager<GameDataManager>();
            InternalConstructor(mag.GetTable<TTable>());
        }
        private void InternalConstructor(TTable table)
        {
            if (table == null)
            {
                LogHelper.LogWarning($"{this.GetType().Name}의 생성자 매개변수가 null입니다.");
                return;
            }
            _table = table;
            _recordDict = _table.GetCachedTableDict();
        }

        public TRecord GetRecord(int id)
        {
            if (!_recordDict.TryGetValue(id, out TRecord record))
            {
                LogHelper.LogWarning($"{this.GetType().Name}.{nameof(GetRecord)} Failed");
            }
            return record;
        }
        public bool TryGetRecord(int id, out TRecord record)
        {
            record = GetRecord(id);
            return record is not null;
        }

        public List<TRecord> GetRecords(System.Func<TRecord, bool> predicate)
        {
            List < TRecord > result = new();
            foreach (var record in _recordDict.Values)
            {
                if(predicate(record)) result.Add(record);
            }
            return result;
        }

        //DataTableHandler<TestTable, TestRecord> TestTablehandler;
        //public void testInitialzie()
        //{
        //    var dataManager = CoreFacade.GetManager<GameDataManager>();
        //    TestTablehandler = new(dataManager.GetTable<TestTable>());
        //}

        //public void test()
        //{
        //}
    }

}
