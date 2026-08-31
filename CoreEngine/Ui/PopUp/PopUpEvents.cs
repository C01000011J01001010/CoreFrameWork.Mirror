using CoreEngine.EventBus;
using System;

namespace CoreEngine.Ui
{
    public enum PopUpType { Confirm, AcceptReject }

    public struct SpawnPopUpEvent : IEvent
    {
        public string Message;
        public PopUpType PopUpType;
        public Action OnConfirm;

        public SpawnPopUpEvent(string message, PopUpType popUpType, Action onConfirm = null)
        {
            Message = message;
            PopUpType = popUpType;
            OnConfirm = onConfirm;
        }
    }
}