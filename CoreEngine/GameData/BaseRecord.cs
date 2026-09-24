using UnityEngine;

namespace CoreEngine.GameData
{
    public interface IRecord 
    { 
        int Index { get; } 
    }
    /// <summary>
    /// <para>규칙1: 데이터 필드는 protected로 선언하며, 이름은 소문자로 시작한다.</para>
    /// <para>규칙2: 데이터 필드에 대한 접근 프로퍼티는 public으로 선언하며, 이름은 대문자로 시작한다.</para>
    /// </summary>
    [System.Serializable]
    public class BaseRecord: IRecord
    {
        protected int index;

        public int Index => index;
    }
}

