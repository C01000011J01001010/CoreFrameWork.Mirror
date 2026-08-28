using TMPro;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.UI;

namespace CoreEngine.Data
{
    /// <summary>
    /// DataContainer(Model)객체의 View역할을 하는 객체의 기본 클래스
    /// </summary>
    /// <typeparam name="DataContainer">보여주는 대상</typeparam>
    /// <typeparam name="Data">대상이 갖는 정적데이터</typeparam>
    public abstract class BaseDataViewer<DataContainer, Data> : BaseObjectViewer<DataContainer>
        where DataContainer : BaseDataContainer<Data>
        where Data : BaseData
    {
        protected virtual void UpdateView_FromStaticData(Data newTarget) { }

        public override void UpdateView()
        {
            if (IsEmpty()) return;
            UpdateView_FromStaticData(ConnectObject.Get());
        }
    }
}


