using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="BaseData">그 대상이 갖는 정보</typeparam>
    /// <typeparam name="Container">보여주는 대상</typeparam>
    public class BaseDataViewer_ForUi<Container, BaseData> : BaseDataViewer<Container, BaseData>
        where Container : BaseDataContainer<BaseData>
        where BaseData : BaseData_ForUi
    {
        [SerializeField] protected TextMeshProUGUI nameText, descriptionText;
        [SerializeField] protected Image iconImage;

        protected override void UpdateView_FromStaticData(BaseData staticData)
        {
            if (staticData == null) return;
            SetDescription(staticData);
            SetName(staticData);
            SetIcon(staticData);
        }
        public virtual void SetDescription(BaseData newTarget) 
        { 
            if(descriptionText)
            {
                descriptionText.text = newTarget.GetDescription();
            }
        }
        public virtual void SetName(BaseData newTarget) 
        {
            if (nameText)
            {
                nameText.text = newTarget.GetInfoName();
            }
        }
        public virtual void SetIcon(BaseData newTarget) 
        { 
            if(iconImage)
            {
                iconImage.sprite = newTarget.GetIcon();
            }
        }
    }
}