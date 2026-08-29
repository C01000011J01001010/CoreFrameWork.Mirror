using System;
using CoreEngine.EventBus;
using CoreEngine.Helpers;

namespace CoreEngine.Interface
{
    /// <summary>
    /// 상속 없이 어떤 클래스에서든 자유롭게 생성(new)하여 사용할 수 있는 인터페이스 수신기
    /// </summary>
    public class InterfaceReceiver<TInterface> : IBindable
        where TInterface : class
    {
        public TInterface Target { get; private set; }

        // 타겟이 설정되었을 때 외부(주인)에게 알려줄 콜백 이벤트
        public event Action<TInterface> OnTargetSet;

        // 수신기 켜기 (주인의 OnEnable 등에서 호출)
        public void Bind()
        {
            EventBus<SetProviderEvent<TInterface>>.Subscribe(OnTargetSetInternal);
            // 켜지자마자 "제공자 있나요?" 하고 핑을 날림
            EventBus<RequestProviderEvent<TInterface>>.Publish(new RequestProviderEvent<TInterface>());
        }

        // 수신기 끄기 (주인의 OnDisable 등에서 호출)
        public void Unbind()
        {
            EventBus<SetProviderEvent<TInterface>>.Unsubscribe(OnTargetSetInternal);
            Target = null;
        }

        private void OnTargetSetInternal(SetProviderEvent<TInterface> evt)
        {
            Target = evt.Provider;
            OnTargetSet?.Invoke(Target);
        }

        public bool TryGet(out TInterface target)
        {
            if (SystemHelper.isUnityNull(Target))
            {
                target = null;
                return false;
            }
            else
            {
                target = Target;
                return true;
            }
        }
    }
}