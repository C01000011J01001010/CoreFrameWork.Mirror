using CoreEngine.EventBus;
using System;

namespace CoreEngine.Interface
{
    /// <summary>
    /// 상속 없이 어떤 클래스에서든 자유롭게 생성하여 인터페이스를 외부로 제공(Publish)하는 부품
    /// </summary>
    public class InterfacePublisher<TInterface> : IBindable
        where TInterface : class
    {
        private TInterface _provider;

        // 생성자: 이 부품을 가진 주인이 자기 자신(this)을 넘겨줍니다.
        public InterfacePublisher(TInterface provider)
        {
            if (!typeof(TInterface).IsInterface)
                throw new ArgumentException($"{typeof(TInterface).Name} must be an interface.");

            _provider = provider;
        }

        // 발행기 켜기 (주인의 OnEnable 등에서 호출)
        public void Bind()
        {
            // 켜지자마자 허공에 명함(인터페이스)을 뿌림 (이미 켜져 있는 Receiver들을 위해)
            EventBus<SetProviderEvent<TInterface>>.Publish(new SetProviderEvent<TInterface>(_provider));

            // 나중에 켜진 Receiver가 "제공자 있나요?" 하고 물어보면 대답하기 위해 구독
            EventBus<RequestProviderEvent<TInterface>>.Subscribe(OnProviderRequested);
        }

        // 발행기 끄기 (주인의 OnDisable 등에서 호출)
        public void Unbind()
        {
            EventBus<RequestProviderEvent<TInterface>>.Unsubscribe(OnProviderRequested);

            // (선택 사항) 내가 꺼질 때 null을 보내서 Receiver들의 Target을 안전하게 비워줌 (안전성 극대화)
            EventBus<SetProviderEvent<TInterface>>.Publish(new SetProviderEvent<TInterface>(null));
        }

        // 누군가 핑(Ping)을 날리면 퐁(Pong)으로 내 명함을 다시 날려줌
        private void OnProviderRequested(RequestProviderEvent<TInterface> evt)
        {
            EventBus<SetProviderEvent<TInterface>>.Publish(new SetProviderEvent<TInterface>(_provider));
        }
    }
}