using CoreEngine.Facades;
using CoreEngine.GameData;
using CoreEngine.GameData.Test;
using CoreEngine.Helpers;
using System.Collections.Generic;
using UnityEngine;

public class DataTableHandler<TTable, TRecord>
    where TTable : BaseDataTable<TRecord>
    where TRecord : class, IDataRecord, new()
{
    private static TTable _table;
    private Dictionary<int, TRecord> _recordDict;
    public DataTableHandler(TTable table)
    {
        if (table == null)
        {
            LogHelper.LogWarning($"{this.GetType().Name}의 생성자 매개변수가 null입니다.");
            return;
        }
        _table = table;
        _recordDict = _table.GetCachedTableDict();
    }
    public _DataTable GetTable() => _table;
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

    //DataTableHandler<TestTable, TestRecord> TestTablehandler;
    //public void testInitialzie()
    //{
    //    var dataManager = CoreFacade.GetManager<GameDataManager>();
    //    TestTablehandler = new(dataManager.GetTable<TestTable>());
    //}

    //public void test()
    //{
    //    TestTablehandler.GetRecord(1);
    //}
}
