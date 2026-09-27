using CoreEngine.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    

    /// <summary>
    /// csv 컨버터에서 사용할 인터페이스
    /// </summary>
    public interface IDataTableSetter
    {
        void SetCapacity(int count);
        bool Add(IDataRecord record);
        void Clear();
        List<IDataRecord> GetListCopy();
    }
    public abstract class BaseDataTable<TRecord> : _DataTable, IDataTableSetter
        where TRecord : BaseDataRecord, IDataRecord, new()
    {
        [SerializeField]
        private List<TRecord> _table = new();

        private Dictionary<int, TRecord> _tableDict = null;

        // int를 index로 하는 dictionary 반환
        public Dictionary<int, TRecord> GetCachedTableDict()
        {
            // 이미 캐시를 했다면
            if (_tableDict != null) return _tableDict;

            // 캐시가 필요하다면
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

        #region ITableSetter
        private static readonly IComparer<TRecord> _indexComparer =
            Comparer<TRecord>.Create((x, y) => x.Id.CompareTo(y.Id));

        void IDataTableSetter.SetCapacity(int count)
        {
            if (count > _table.Capacity)
                _table.Capacity = count;
        }

        /// <summary>
        /// 정렬 삽입
        /// </summary>
        bool IDataTableSetter.Add(IDataRecord newRecord)
        {
            // 타입방어 및 캐스팅
            if (newRecord is not TRecord recordAsT) return false;

            int listIndex = _table.BinarySearch(recordAsT, _indexComparer);

            // 동일한 Primary Key가 이미 존재
            if (listIndex >= 0) return false;

            // BinarySearch 결과를 실제 삽입 위치로 변환
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
        #endregion
    }
}


