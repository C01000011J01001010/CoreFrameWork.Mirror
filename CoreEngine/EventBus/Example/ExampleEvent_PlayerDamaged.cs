// example -> 주고받을 이벤트 명세서
using CoreEngine.EventBus;

namespace CoreEngine.EventBus.Example
{
    public struct ExampleEvent_PlayerDamaged : IEvent
    {
        public int PlayerId;
        public int CurrentHp;
        public int MaxHp;
    }
}
