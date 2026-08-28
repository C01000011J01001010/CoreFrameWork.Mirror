using UnityEngine;
using System;

namespace CoreEngine.Actor
{
    /// <summary>
    /// 모든 인게임 엔티티(Actor)의 최상위 껍데기 예시
    /// </summary>
    [DisallowMultipleComponent]
    public class SampleActorHost : BaseActor, IActorHost, ITickable
    {
        public TickGroup TickGroup => TickGroup.Character;
        private SampleActorFeature _sampleActorComponent = new();

        private void Awake()
        {
            _sampleActorComponent.Initialize(this);
        }

        public void Tick(float deltaTime)
        {
            // 필요에 따라 순서 제어하여 호출
            _sampleActorComponent.Tick(deltaTime);
        }

        public bool TryGetFeature<T>(out T part) where T : class, IActorFeature
        {
            switch (typeof(T))
            {
                case Type t when t == typeof(SampleActorFeature):
                    part = _sampleActorComponent as T;
                    return true;
                default:
                    part = default;
                    return false;
            }
        }
    }
}
