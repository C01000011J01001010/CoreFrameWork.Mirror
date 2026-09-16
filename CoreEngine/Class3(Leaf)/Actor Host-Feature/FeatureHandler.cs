using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using CoreEngine.Pool;

namespace CoreEngine.Actor
{
    public class FeatureHandler
    {
        /// <summary>
        /// TryGetFeature을 편하게 하기 위한 자료구조
        /// </summary>
        protected readonly Dictionary<Type, IActorFeature> _featureMap = new();
        protected readonly List<IActorFeature> _featureList = new();
        protected readonly List<ISpawnable> _spawnableList = new();
        protected readonly List<ITick> _tickableFeatures = new();
        protected readonly List<ILateTick> _lateTickableFeatures = new();
        protected readonly List<IFixedTick> _fixedTickableFeatures = new();

        protected IActorHost _host;

        public bool SetHost(IActorHost host)
        {
            if (host == null)
                return false;

            if (_host != null)
                return false;

            _host = host;
            return true;
        }


        /// <summary>
        /// Actor의 기능 등록을 위한 필수 절차
        /// </summary>
        public bool RegisterFeature(IActorFeature feature)
        {
            if (feature == null)
            {
                LogHelper.LogWarning($"{_host?.name}.{GetType().Name}에서 null feature 등록 시도");
                return false;
            }

            if (_featureMap.TryAdd(feature.GetType(), feature))
            {
                _featureList.Add(feature);
                return true;
            }
            else
            {
                LogHelper.LogWarning($"{_host?.name}.{GetType().Name}에서 중복 기능 등록 시도");
                return false;
            }
        }

        /// <summary>
        /// 제네릭으로 객체 생성후 <see cref="RegisterFeature"/> 사용
        /// </summary>
        public bool RegisterFeature<T>() where T : IActorFeature, new()
        {
            return RegisterFeature(new T());
        }

        /// <summary>
        /// <para>등록된 전체 <see cref="IActorFeature"/> 중 초기화되지 않은 객체를 초기화</para>
        /// <para><see cref="RegisterFeature"/>를 통한 등록 순서 사용</para>
        /// <para>내부적으로 <see cref="Initialize_RegisteredFeature"/> 사용</para>
        /// </summary>
        public void Initialize_RegisteredFeatures()
        {
            var features = GetFeatureArrayCopy();
            for (int i = 0; i < features.Length; i++)
            {
                Initialize_RegisteredFeature(features[i]);
            }
        }

        /// <summary>
        /// 등록된 <see cref="IActorFeature"/>를 개별 초기화
        /// </summary>
        public void Initialize_RegisteredFeature<T>() where T : class, IActorFeature
        {
            TryGetFeature(out T feature);
            Initialize_RegisteredFeature(feature);
        }

        /// <summary>
        /// 등록된 <see cref="IActorFeature"/>를 개별 초기화
        /// </summary>
        public void Initialize_RegisteredFeature(IActorFeature feature)
        {
            if (SystemHelper.isUnityNull(feature))
            {
                LogHelper.LogError($"null 객체({nameof(IActorFeature)}) 초기화 시도");
                return;
            }
            if (feature.IsInit)
            {
                LogHelper.LogWarning($"{nameof(IActorFeature)}({feature.GetType().Name})은 이미 초기화됨");
                return;
            }
            if (_host == null)
            {
                LogHelper.LogError($"{nameof(IActorFeature)} 초기화를 위한 {nameof(IActorHost)}가 존재하지 않음");
                return;
            }
            if (!_featureMap.ContainsKey(feature.GetType()))
            {
                LogHelper.LogError($"등록되지 않은 {nameof(IActorFeature)}({feature.GetType().Name})의 초기화 시도");
                return;
            }

            // 초기화 후 틱 리스트에 넣기
            feature.Initialize(_host);
            if (feature is ISpawnable asSpawnable) _spawnableList.Add(asSpawnable);
            if (feature is ITick asTick) _tickableFeatures.Add(asTick);
            if (feature is ILateTick asLateTick) _lateTickableFeatures.Add(asLateTick);
            if (feature is IFixedTick asFixedTick) _fixedTickableFeatures.Add(asFixedTick);
        }

        /// <summary>
        /// <see cref="RegisterFeature"/>등록 순서 역순의 
        /// <see cref="IDisposable.Dispose"/>Feature를 실행
        /// </summary>
        public void Dispose_RegisteredFeatures()
        {
            var features = GetFeatureArrayCopy();
            for (int i = features.Length - 1; i >= 0; i--)
            {
                if (features[i] is IDisposable disposable) disposable.Dispose();
            }

            _featureMap.Clear();
            _featureList.Clear();
            _tickableFeatures.Clear();
            _lateTickableFeatures.Clear();
            _fixedTickableFeatures.Clear();

        }

        /// <summary>
        /// <see cref="Initialize_RegisteredFeature"/>초기화 순서의 
        /// <see cref="ISpawnable.OnSpawn"/>Feature를 실행
        /// </summary>
        public void OnSpawn_InitializedFeatures()
        {
            for(int i = 0; i < _spawnableList.Count; i++)
            {
                _spawnableList[i].OnSpawn();
            }
        }

        /// <summary>
        /// <see cref="Initialize_RegisteredFeature"/>초기화 순서의 
        /// <see cref="ISpawnable.OnDespawn"/>Feature를 실행
        /// </summary>
        public void OnDespawn_InitializedFeatures()
        {
            for (int i = 0; i < _spawnableList.Count; i++)
            {
                _spawnableList[i].OnDespawn();
            }
        }

        /// <summary>
        /// <see cref="Initialize_RegisteredFeature"/>초기화 순서의 
        /// <see cref="ITick.Tick(float)"/>Feature를 실행
        /// </summary>
        public void Tick_InitializedFeatures(float deltaTime)
        {
            for (int i = 0; i < _tickableFeatures.Count; i++)
            {
                _tickableFeatures[i].Tick(deltaTime);
            }
        }

        /// <summary>
        /// <see cref="Initialize_RegisteredFeature"/>초기화 순서의 
        /// <see cref="ILateTick.LateTick(float)"/> Feature를 실행
        /// </summary>
        public void LateTick_InitializedFeatures(float deltaTime)
        {
            for (int i = 0; i < _lateTickableFeatures.Count; i++)
            {
                _lateTickableFeatures[i].LateTick(deltaTime);
            }
        }

        /// <summary>
        /// <see cref="Initialize_RegisteredFeature"/>초기화 후 초기화 순서의 
        /// <see cref="IFixedTick.FixedTick(float)"/> Feature를 실행
        /// </summary>
        public void FixedTick_InitializedFeatures(float fixedDeltaTime)
        {
            for (int i = 0; i < _fixedTickableFeatures.Count; i++)
            {
                _fixedTickableFeatures[i].FixedTick(fixedDeltaTime);
            }
        }


        /// <summary>
        /// 객체를 받기 위해 정확한 타입을 입력해야함
        /// </summary>
        public bool TryGetFeature<T>(out T feature) where T : class, IActorFeature
        {
            if (_featureMap.TryGetValue(typeof(T), out var value))
            {
                feature = value as T;
                return feature != null;
            }

            feature = null;
            return false;
        }

        //----------------------------------------------------------------------

        /// <summary>
        /// <see cref="RegisterFeature"/> 실행 순서를 기억하는 배열 반환
        /// </summary>
        protected IActorFeature[] GetFeatureArrayCopy()
        {
            // 리스트 순회 중에 터지지 않도록 복사
            return _featureList.ToArray();
        }
    }
}
