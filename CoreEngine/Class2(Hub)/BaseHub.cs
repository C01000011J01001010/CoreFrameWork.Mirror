using CoreEngine.EventBus;
using System.Collections;
using UnityEngine;

namespace CoreEngine.Hub
{
    public interface IRegistration
    {
        public bool isAdd { get; }
        public ContextScope scope { get;}
    }

    public abstract class BaseHub<TRegistrationEvent> : MonoBehaviour//, IInitialize, ILateInitialize
        where TRegistrationEvent : struct, IEvent, IRegistration
    {
        public ContextScope myScope { get; private set; }

        internal void SetScope(ContextScope scope)
        {
            myScope = scope;
        }

        internal virtual void OnEnableFromContext() 
        {
            EventBus<TRegistrationEvent>.Subscribe(OnLeafRegistration);
        }
        internal virtual void OnDisableFromContext() 
        {
            EventBus<TRegistrationEvent>.Unsubscribe(OnLeafRegistration);
        }

        public virtual IEnumerator Initialize() { yield break; }
        public virtual IEnumerator LateInitialize() { yield break; }

        protected virtual void OnLeafRegistration(TRegistrationEvent evt)
        {
            if (!IsMyScope(evt.scope)) return;
            if (evt.isAdd) RegisterLeaf(evt);
            else UnregisterLeaf(evt);
        }

        protected bool IsMyScope(ContextScope scope)
        {
            // 내 영역 아니면 버리기
            if (myScope == ContextScope.Scene)
            {
                // Scene 허브: Project만 아니면 다 받음 (Scene, None 수용) -> OK
                if (scope == ContextScope.Project) return false; 
            }
            else
            {
                // Project 허브: Project가 아니면 다 버림 (Scene 버림, None도 버림!)
                if (scope != ContextScope.Project) return false; 
            }
            return true;
        }

        protected abstract void RegisterLeaf(TRegistrationEvent evt);

        protected abstract void UnregisterLeaf(TRegistrationEvent evt);
    }
}

