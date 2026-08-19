using CoreEngine.EventBus;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine
{
    public abstract class CoreMonoBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable() => RegisterTick();
        protected virtual void OnDisable() => UnregisterTick();

        protected void RegisterTick()
        {
            if (this is ITickable tickable)
            {
                EventBus<R_TickEvent>.Publish(new R_TickEvent(tickable, tickable.TickGroup, true));
            }

            if (this is ILateTickable lateTickable)
            {
                EventBus<R_LateTickEvent>.Publish(new R_LateTickEvent(lateTickable, lateTickable.LateTickGroup, true));
            }

            if (this is IFixedTickable fixedTickable)
            {
                EventBus<R_FixedTickEvent>.Publish(new R_FixedTickEvent(fixedTickable, fixedTickable.FixedTickGroup, true));
            }
        }

        protected void UnregisterTick()
        {
            if (this is ITickable tickable)
                EventBus<R_TickEvent>.Publish(new R_TickEvent(tickable, tickable.TickGroup, false));

            if (this is ILateTickable lateTickable)
                EventBus<R_LateTickEvent>.Publish(new R_LateTickEvent(lateTickable, lateTickable.LateTickGroup, false));

            if (this is IFixedTickable fixedTickable)
                EventBus<R_FixedTickEvent>.Publish(new R_FixedTickEvent(fixedTickable, fixedTickable.FixedTickGroup, false));
        }
    }
}
