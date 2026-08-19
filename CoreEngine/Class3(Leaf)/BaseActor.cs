using CoreEngine.EventBus;
using CoreEngine.Hub;

namespace CoreEngine
{
    public abstract class BaseActor : BaseLeaf, IActor
    {

        protected override void OnEnable()
        {
            base.OnEnable();
            // Hub에 내가 등록됐음을 알림
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
