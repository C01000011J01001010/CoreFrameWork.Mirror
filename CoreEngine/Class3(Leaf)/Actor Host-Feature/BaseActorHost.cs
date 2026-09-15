using System;
using System.Collections.Generic;
using UnityEngine.AdaptivePerformance.Provider;

namespace CoreEngine.Actor
{
    public class BaseActorHost : BaseActor, IActorHost
    {
        /// <summary>
        /// TryGetFeature을 편하게 하기 위한 자료구조
        /// </summary>
        protected readonly Dictionary<Type, IActorFeature> FeatureMap = new();

        private readonly List<IActorFeature> _featureList = new();
        private readonly List<ITickable> _tickableFeatures = new();
        private readonly List<IFixedTickable> _fixedTickableFeatures = new();

        

        protected void RegisterFeature(IActorFeature feature)
        {
            if (feature == null)
                return;

            if (FeatureMap.TryAdd(feature.GetType(), feature))
            {
                _featureList.Add(feature);
                
            }
        }


        /// <summary>
        /// <see cref="RegisterFeature"/> 실행 순서를 기억하는 배열 반환
        /// </summary>
        protected IActorFeature[] GetFeatureArrayCopy()
        {
            // 리스트 순회 중에 터지지 않도록 복사
            return _featureList.ToArray();
        }



        /// <summary>
        /// <para>등록된 전체 Feature를 초기화</para>
        /// <para><see cref="RegisterFeature"/>를 통한 등록 순서 사용</para>
        /// </summary>
        protected void InitializeAddedFeature()
        {
            var features = GetFeatureArrayCopy();
            for (int i= 0; i < features.Length; i++ )
            {
                if (!features[i].IsInit)
                {
                    // 초기화 후 틱 리스트에 넣기
                    features[i].Initialize(this);
                    if (features[i] is ITickable asTick) _tickableFeatures.Add(asTick);
                    if (features[i] is IFixedTickable asFixedTick) _fixedTickableFeatures.Add(asFixedTick);
                }
            }
        }

        /// <summary>
        /// <para>등록된 전체 Feature의 리소스를 정리</para>
        /// <para><see cref="RegisterFeature"/>를 통한 등록 순서 역순 사용</para>
        /// </summary>
        protected void DisposeAddedFeature()
        {
            var features = GetFeatureArrayCopy();
            for (int i = features.Length-1; i >= 0; i--)
            {
                if (features[i] is IDisposable disposable) disposable.Dispose();
            }    
        }

        /// <summary>
        /// <see cref="RegisterFeature"/> 등록 순서대로 등록된 <see cref="ITickable"/> Feature를 Tick한다.
        /// </summary>
        protected void TickAddedFeature(float deltaTime)
        {
            for(int i = 0; i< _tickableFeatures.Count; i++)
            {
                _tickableFeatures[i].Tick(deltaTime);
            }
        }

        /// <summary>
        /// <see cref="RegisterFeature"/> 등록 순서대로 등록된 <see cref="IFixedTickable"/> Feature를 FixedTick한다.
        /// </summary>
        protected void FixedTickAddedFeature(float fixedDeltaTime)
        {
            for (int i = 0; i < _fixedTickableFeatures.Count; i++)
            {
                _fixedTickableFeatures[i].FixedTick(fixedDeltaTime);
            }
        }

        public bool TryGetFeature<T>(out T feature) where T : class, IActorFeature
        {
            if (FeatureMap.TryGetValue(typeof(T), out var value))
            {
                feature = value as T;
                return feature != null;
            }

            feature = null;
            return false;
        }
    }
}

