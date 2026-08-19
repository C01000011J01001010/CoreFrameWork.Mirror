using CoreEngine.EventBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine.Interface
{
    // 모든 인터페이스 제공자가 자신을 등록할 때 쓸 단일 제네릭 이벤트
    public struct SetProviderEvent<TInterface> : IEvent where TInterface : class
    {
        public TInterface Provider;
        public SetProviderEvent(TInterface provider) => Provider = provider;
    }

    // 모든 인터페이스 소비자가 제공자를 찾을 때 쓸 단일 제네릭 이벤트
    public struct RequestProviderEvent<TInterface> : IEvent where TInterface : class
    {
        // 핑(Ping) 역할만 하므로 내용은 비워둡니다.
    }
}
