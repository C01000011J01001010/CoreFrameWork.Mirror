namespace CoreEngine.Actor
{
    /// <summary>
    /// <see cref="IActorFeature"/>의 선택적 추가기능을 모두 적용한 확장 클래스
    /// </summary>
    public abstract class BaseActorFeatureExtended : BaseActorFeature,
        CoreEngine.Pool.ISpawnable,
        System.IDisposable,
        CoreEngine.ITick,
        CoreEngine.IFixedTick,
        CoreEngine.ILateTick
    {
        public virtual void OnSpawn() { }
        public virtual void OnDespawn() { }

        public virtual void Dispose() { }

        public virtual void Tick(float deltaTime) { }
        public virtual void LateTick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
    }
}
