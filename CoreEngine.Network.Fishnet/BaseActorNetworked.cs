using System;
using CoreEngine.EventBus;
using CoreEngine.Hub;

namespace CoreEngine.Network
{
    // 멀티플레이 객체용 3계층 Leaf 기본 클래스

    public abstract class BaseActorNetworked : BaseLeafNetworked, IActor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            var evt = new ActorRegistrationEvent(this, true, myScope);
            EventBus<ActorRegistrationEvent>.Publish(evt);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Hub에 내가 안쓰임을 알림
            var evt = new ActorRegistrationEvent(this, false, myScope);
            EventBus<ActorRegistrationEvent>.Publish(evt);
        }
    }
}

