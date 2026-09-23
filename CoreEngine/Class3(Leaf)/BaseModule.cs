using CoreEngine.EventBus;
using CoreEngine.Hub;
using System.Collections;

namespace CoreEngine
{
    public abstract class BaseModule : BaseLeaf, IModule
    {
        private bool _isInit;
        private bool _isActive;

        bool IModule.IsInit => _isInit;
        bool IModule.IsActive => _isActive;

        bool IModule.GetIsInit() => _isInit;
        bool IModule.GetIsActive() => _isActive;

        IEnumerator IModule.Initialize()
        {
            if (_isInit) yield break;
            yield return OnInitialize();
            _isInit = true;
        }
        protected virtual IEnumerator OnInitialize() { yield break; }

        void IModule.Exit()
        {
            OnExit();
            _isInit = false;
        }
        public virtual void OnExit() { }

        void IModule.SetActive(bool active)
        {
            // 시스템적으로 활성화 여부와 논리적 활성화 여부를 분리
            if (active == GetSystemActive() &&
                active == _isActive)
                return;

            ActiveMethod(active);
            _isActive = active;

            OnSetActive(active);
        }
        protected virtual bool GetSystemActive() {return gameObject.activeInHierarchy; }
        protected virtual void ActiveMethod(bool active) 
        { 
            if(active && gameObject.activeSelf && !gameObject.activeInHierarchy)
            {
                UnityEngine.Transform parent = transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        parent.gameObject.SetActive(true);
                        break;
                    }

                    parent = parent.parent;
                }
            }
            gameObject.SetActive(active); 
        }
        protected virtual void OnSetActive(bool active) { }

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
