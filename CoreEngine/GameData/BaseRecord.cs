using CoreEngine.Helpers;
using System.Collections;
using System.Text;
using UnityEngine;

namespace CoreEngine.GameData
{

    public interface IIdentifiable
    {
        ulong ID { get; }
        
    }
    public interface IRecord : IIdentifiable { }
    internal interface IBakeId
    {
        ulong BakeID();
    }
    /// <summary>
    /// <para>규칙1: 데이터 필드는 protected로 선언하며, 이름은 소문자로 시작한다.</para>
    /// <para>규칙2: 데이터 필드에 대한 접근 프로퍼티는 public으로 선언하며, 이름은 대문자로 시작한다.</para>
    /// </summary>
    [System.Serializable]
    public abstract class BaseRecord: IRecord, IBakeId
    {
#if UNITY_EDITOR
        [SerializeField]
        private string _primarykey;
#endif

        [SerializeField]
        private ulong _hashCode;
        ulong IIdentifiable.ID => _hashCode;

        ulong IBakeId.BakeID() => _hashCode = BakeID();

        protected abstract ulong BakeID();
        protected ulong GetMashedKey(params object[] datas)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < datas.Length; i++)
            {
                if (i > 0) sb.Append('/');

                object data = datas[i];
                if (data == null) continue;

                if (data is IEnumerable EnumerableData && !(data is string))
                {
                    sb.Append('[');
                    bool isFirst = true;
                    foreach (var item in EnumerableData)
                    {
                        if (!isFirst) sb.Append(',');

                        sb.Append(item);
                        isFirst = false;
                    }
                    sb.Append(']');
                }
                else
                {
                    sb.Append(data);
                }
            }

            _primarykey = sb.ToString();
            Debug.Log(_primarykey);
            return HashHelper.StringToId(_primarykey);
        }
    }
}

