using CoreEngine.EventBus;
using UnityEngine;

namespace CoreEngine.Extentions
{
    public static class TickRegistrationExtensions
    {
        /// <summary>
        /// 객체가 상속받은 Tick 인터페이스를 감지하여 EventBus로 자동 구독 요청을 보냄
        /// </summary>
        public static void RegisterTick(this MonoBehaviour mono)
        {
            if (mono is ITickable tickable)
                EventBus<R_TickEvent>.Publish(new R_TickEvent(tickable, tickable.TickGroup, true));

            if (mono is ILateTickable lateTickable)
                EventBus<R_LateTickEvent>.Publish(new R_LateTickEvent(lateTickable, lateTickable.LateTickGroup, true));

            if (mono is IFixedTickable fixedTickable)
                EventBus<R_FixedTickEvent>.Publish(new R_FixedTickEvent(fixedTickable, fixedTickable.FixedTickGroup, true));
        }

        /// <summary>
        /// 객체가 상속받은 Tick 인터페이스를 감지하여 EventBus로 자동 구독 해제 요청을 보냄
        /// </summary>
        public static void UnregisterTick(this MonoBehaviour mono)
        {
            if (mono is ITickable tickable)
                EventBus<R_TickEvent>.Publish(new R_TickEvent(tickable, tickable.TickGroup, false));

            if (mono is ILateTickable lateTickable)
                EventBus<R_LateTickEvent>.Publish(new R_LateTickEvent(lateTickable, lateTickable.LateTickGroup, false));

            if (mono is IFixedTickable fixedTickable)
                EventBus<R_FixedTickEvent>.Publish(new R_FixedTickEvent(fixedTickable, fixedTickable.FixedTickGroup, false));
        }
    }
}