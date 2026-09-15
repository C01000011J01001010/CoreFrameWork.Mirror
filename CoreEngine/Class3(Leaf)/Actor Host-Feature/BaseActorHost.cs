using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine.AdaptivePerformance.Provider;

namespace CoreEngine.Actor
{
    public abstract class BaseActorHost : BaseActor, IActorHost
    {
        protected readonly FeatureHandler FeatureHandler = new();

        protected virtual void Awake()
        {
            FeatureHandler.SetHost(this);
        }
        
        public bool TryGetFeature<T>(out T feature) where T : class, IActorFeature
        {
            return FeatureHandler.TryGetFeature(out feature);
        }
    }
}

