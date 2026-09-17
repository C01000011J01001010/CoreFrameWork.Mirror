using CoreEngine.Actor;
using CoreEngine.Pool;
using UnityEngine;

namespace CoreEngine.Actor
{
    public abstract class BaseActorHostExtended : BaseActorHost, ISpawnable
    {
        protected override void Awake()
        {
            base.Awake();
            RegisterFeatures();
            FeatureHandler.Initialize_RegisteredFeatures();
        }
        protected abstract void RegisterFeatures();

        public virtual void OnSpawn()
        {
            FeatureHandler.OnSpawn_InitializedFeatures();
        }
        public virtual void OnDespawn()
        {
            FeatureHandler.OnDespawn_InitializedFeatures();
        }
        protected virtual void OnDestroy()
        {
            FeatureHandler.Dispose_RegisteredFeatures();
        }
    }
}