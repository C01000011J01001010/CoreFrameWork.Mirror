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
        [SerializeField]
        protected ulong _hashCode;

        ulong IIdentifiable.ID => _hashCode;

        ulong IBakeId.BakeID() => _hashCode = BakeID();

        protected abstract ulong BakeID();
        protected ulong GetMashedKey(params object[] datas)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < datas.Length; i++)
            {
                if (i > 0) sb.Append('/'); // 요소 간의 구분자

                object data = datas[i];
                if (data == null) continue;

                // 만약 데이터가 배열이나 리스트 같은 컬렉션이라면? (단, string은 문자들의 배열이지만 예외 처리)
                if (data is IEnumerable enumerable && !(data is string))
                {
                    bool isFirstItem = true;
                    foreach (var item in enumerable)
                    {
                        if (!isFirstItem) sb.Append(','); // 배열 내부 요소 간의 구분자
                        sb.Append(item);
                        isFirstItem = false;
                    }
                }
                else
                {
                    // 일반 단일 값
                    sb.Append(data);
                }
            }

            return HashHelper.StringToId(sb.ToString());
        }
    }
}

