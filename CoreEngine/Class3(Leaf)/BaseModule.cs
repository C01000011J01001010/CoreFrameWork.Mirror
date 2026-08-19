using CoreEngine.EventBus;
using CoreEngine.Hub;
using System.Collections;

namespace CoreEngine
{
    public abstract class BaseModule : BaseLeaf, IModule
    {
        private bool isActive;
        public bool IsActive => isActive;

        public virtual void Exit() { }

        public virtual IEnumerator Initialize() { yield return null; }

        public virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
            isActive = active;
        }

        protected virtual void Awake()
        {
            var evt = new ModuleRegistrationEvent(this, true, myScope);
            EventBus<ModuleRegistrationEvent>.Publish(evt);
        }

        protected virtual void OnDestroy()
        {
            // 만약 Hub가 먼저 사라졌다해도 문제 없음
            var evt = new ModuleRegistrationEvent(this, false, myScope);
            EventBus<ModuleRegistrationEvent>.Publish(evt);
        }
    }
}
