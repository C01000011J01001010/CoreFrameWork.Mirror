using System.Collections;
using UnityEngine;
using CoreEngine.Extensions;

namespace CoreEngine
{
    public abstract class BaseMonoModule<TOnwer> : MonoBehaviour//, IModule
    where TOnwer : MonoBehaviour, IModuleHub
    {
        public TOnwer Owner { get; private set; }

        public bool IsActive { get; protected set; }

        public virtual void Exit()
        {
            SetActive(false);
        }

        public virtual IEnumerator Initialize(IModuleHub hub)
        {
            Owner = hub.AsOrThrow<TOnwer>();
            yield break;
        }

        public virtual void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}
