using UnityEngine;
using CoreEngine.Extentions;

namespace CoreEngine.Data
{
    // 게임 내에서 혼자서 형태를 갖지 않는 정보객체
    public abstract class BaseData_ForUi : BaseData
    {
        [TextArea(3, 5)]
        [SerializeField] private string description; // ui띄울 설명

        [SerializeField] private Sprite icon; // ui에 띄울 아이콘

        public virtual string GetDescription() => description.ToReflectionText(this);
        public virtual Sprite GetIcon() => icon;
    }
}

