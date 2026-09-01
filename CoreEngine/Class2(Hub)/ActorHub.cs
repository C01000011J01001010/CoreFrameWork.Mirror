using CoreEngine.EventBus;
using System;
using System.Collections;
using System.Collections.Generic;
using CoreEngine.Helpers;
using System.Linq;
using UnityEngine;

namespace CoreEngine.Hub
{
    // 1. 아주 가벼운 마커 인터페이스. 모든 Leaf Actor는 이를 구현합니다.
    public interface IActor { }

    public interface IActorSpawn : IActor
    {
        void OnSpawn();
        void OnDespawn();
    }

    // 2. 클래스 제네릭을 제거한 통합 등록 이벤트
    public struct ActorRegistrationEvent : IEvent, IRegistration
    {
        public IActor actor;
        public bool isAdd { get; private set; }
        public ContextScope scope { get; private set; }

        public ActorRegistrationEvent(IActor actor, bool isAdd, ContextScope scope)
        {
            this.actor = actor;
            this.isAdd = isAdd;
            this.scope = scope;
        }
    }

    // 3. 단 하나만 존재하며, 서브 클래스가 필요 없는 통합 ActorHub
    public sealed class ActorHub : BaseHub<ActorRegistrationEvent>
    {
        // 씬Context 시퀀스 보장용 우선순위 (이제 액터는 단 하나이므로 기본값 고정)
        //public int Priority => 200; 

        // [핵심] 클래스 타입 및 인터페이스 타입별로 액터를 자동 분류하는 전화번호부
        private readonly Dictionary<Type, HashSet<IActor>> _actorRegistry = new();

        public override IEnumerator Initialize()
        {
            LogHelper.LogFunctionCallStart(this);
            return base.Initialize();
        }

        public override IEnumerator LateInitialize()
        {
            LogHelper.LogFunctionCallStart(this);
            return base.LateInitialize();
        }

        internal override void AwakeFromContext()
        {
            base.AwakeFromContext();
            EventBus<ActorRegistrationEvent>.Subscribe(OnLeafRegistration);
        }

        internal override void OnDestroyFromContext()
        {
            base.OnDestroyFromContext();
            EventBus<ActorRegistrationEvent>.Unsubscribe(OnLeafRegistration);
            _actorRegistry.Clear();
        }

        //public override IEnumerator Initialize() => null;
        //public override IEnumerator LateInitialize() => null;


        protected override void RegisterLeaf(ActorRegistrationEvent evt)
        {
            IActor actor = evt.actor;

            Type actorType = actor.GetType();

            // 실제 구체 클래스 타입(예: CapsuleMan)으로 등록
            AddToRegistry(actorType, actor);

            // 해당 액터가 구현한 모든 상위 인터페이스(예: ICharacter, IDamageable 등)로도 자동 등록
            Type[] interfaces = actorType.GetInterfaces();
            foreach (Type @interface in interfaces)
            {
                // IActor를 상속받은 커스텀 인터페이스들만 추려내어 등록
                // 이걸 위해서 IActor는 비어있는 인터페이스로 사용
                if (typeof(IActor).IsAssignableFrom(@interface) && @interface != typeof(IActor))
                {
                    AddToRegistry(@interface, actor);
                }
            }
        }

        protected override void UnregisterLeaf(ActorRegistrationEvent evt)
        {
            IActor actor = evt.actor;

            Type actorType = actor.GetType();
            RemoveFromRegistry(actorType, actor);

            Type[] interfaces = actorType.GetInterfaces();
            foreach (Type @interface in interfaces)
            {
                if (typeof(IActor).IsAssignableFrom(@interface) && @interface != typeof(IActor))
                {
                    RemoveFromRegistry(@interface, actor);
                }
            }
        }

        private void AddToRegistry(Type type, IActor actor)
        {
            if (!_actorRegistry.TryGetValue(type, out var set))
            {
                set = new HashSet<IActor>();
                _actorRegistry[type] = set;
            }
            set.Add(actor);
        }

        private void RemoveFromRegistry(Type type, IActor actor)
        {
            if (_actorRegistry.TryGetValue(type, out var set))
            {
                set.Remove(actor);
                if (set.Count == 0) _actorRegistry.Remove(type);
            }
        }

        // ==========================================
        //  외부에서 다른 객체들이 액터를 조회하는 초간단 쿼리 API
        // ==========================================

        /// <summary>
        /// 특정 인터페이스나 클래스 타입의 모든 활성 액터를 안전하게 가져옵니다.
        /// </summary>
        public IEnumerable<T> GetActors<T>() where T : IActor
        {
            if (_actorRegistry.TryGetValue(typeof(T), out var set))
            {
                // HashSet에 있는 요소들을 T타입으로 캐스팅한 후, 완벽한 복사본(Array)으로 만들어 반환
                return set.Cast<T>().ToArray();
            }

            // 딕셔너리에 타입이 아예 없을 때는 널(null) 대신 빈 배열을 줘서 
            // 호출하는 쪽에서 널 체크 없이 편하게 foreach
            return System.Array.Empty<T>();
        }

        /// <summary>
        /// 특정 타입의 액터 중 첫 번째 요소를 가져옵니다. (단일 객체 조회용)
        /// </summary>
        public T GetActor<T>() where T : class, IActor
        {
            if (_actorRegistry.TryGetValue(typeof(T), out var set))
            {
                foreach (var actor in set)
                {
                    return actor as T;
                }
            }
            return null;
        }
    }
}