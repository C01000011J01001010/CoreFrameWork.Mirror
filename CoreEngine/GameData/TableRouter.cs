using UnityEngine;
using System.Collections.Generic;
using System;

namespace CoreEngine.GameData
{
    public static class TableRouter
    {
        private readonly static Dictionary<Type, _Table> _tableMap = new();

        // GameDataManager가 어드레서블로 로드한 뒤 이 메서드를 호출해 주입
        public static void InjectTable(_Table table)
        {
            if (_tableMap.ContainsKey(table.GetType()))
            {
                Debug.LogWarning($"Table of type {table.GetType().Name} is already injected.");
                return;
            }
            _tableMap[table.GetType()] = table;
            table.InitializeRuntimeCache();
        }

        public static TTable GetTable<TTable>() where TTable : _Table
            => GetTable(typeof(TTable)) as TTable;
        public static _Table GetTable(Type tableType)
        {
            if (_tableMap.TryGetValue(tableType, out _Table table))
            {
                return table;
            }
            return null;
        }

        public static IRecord GetRecord(Type tableType, int id)
        {
            if (_tableMap.TryGetValue(tableType, out _Table table))
            {
                return table.GetRecord(id);
            }
            return null;
        }
    }
}
