using UnityEngine;
using CoreEngine;
using System;

namespace CoreEngine
{
    public abstract class BaseUi : BaseModule, IUi
    {
        public void Show()
        {
            ShowInternal();
            OnShow();
        }
        protected virtual void ShowInternal() { SetActive(true); }
        protected virtual void OnShow() { }

        public virtual void Hide()
        {
            HideInternal();
            OnHide();
        }
        protected virtual void HideInternal() { SetActive(false); }
        protected virtual void OnHide() { }
    }
}
