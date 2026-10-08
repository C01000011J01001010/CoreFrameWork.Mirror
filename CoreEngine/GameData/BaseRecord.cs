using CoreEngine.Helpers;
using System.Collections;
using UnityEngine;

namespace CoreEngine.GameData
{
    public interface IIdentifiable { ulong ID { get; } }
    public interface IRecord : IIdentifiable { }

    /// <summary>
    /// <para>규칙1: 데이터 필드는 protected로 선언하며, 이름은 소문자로 시작한다.</para>
    /// <para>규칙2: 데이터 필드에 대한 접근 프로퍼티는 public으로 선언하며, 이름은 대문자로 시작한다.</para>
    /// </summary>
    [System.Serializable]
    public abstract partial class BaseRecord: IRecord
    {
        [SerializeField]
        private ulong _hashCode;
        ulong IIdentifiable.ID => _hashCode;


        /// <summary>
        /// <para>이 레코드를 고유하게 식별하는 '원자값 문자열(Natural Key)'을 반환해야 합니다.</para>
        /// <para>(배열이나 <see cref="ForeignKey{TRecord, TTable}"/> 객체 등을 여기에 포함해서는 안 됩니다.)</para>
        /// </summary>
        protected abstract string GetPrimaryKey();

        /// <summary>
        /// 만약 [등급+타입]처럼 복합키가 꼭 필요한 경우를 위한 헬퍼 
        /// (구분자 충돌 방지를 위해 <>로 묶어서 반환)
        /// </summary>
        protected string GetCompositeKey(params object[] atomics)
        {
            string[] atomicStrings = new string[atomics.Length];

            for (int i = 0; i < atomics.Length; ++i)
            {
                object element = atomics[i];
                if (element == null) continue;

                if (element is IEnumerable && element is not string)
                {
                    LogHelper.LogError($"[BaseRecord] 복합키 생성 오류: 배열/컬렉션은 원자값 PK로 사용할 수 없습니다. ({element.GetType()})");
                    return null;
                }

                // 괄호로 묶어 문자열 내부의 '_' 충돌 방지
                atomicStrings[i] = $"<{element.ToString()}>";
            }

            return string.Join("_", atomicStrings);
        }
    }
}

