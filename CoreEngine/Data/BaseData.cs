using UnityEngine;
using CoreEngine.Extentions;

namespace CoreEngine.Data
{
    public interface IBaseData
    {
        public int Index { get; }
        public string NameTag { get; }
    }


    // 모든 csv 데이터의 기초형태
    public abstract class BaseData : ScriptableObject, IBaseData
    {
        [SerializeField] private int index;

        [TextArea(1, 3)]
        [SerializeField] private string nameTag = ""; // 표시될 이름

        public int Index => index;

        public string NameTag => nameTag;

        public virtual string GetInfoName() => nameTag.ToReflectionText(this);
    }
}
