using CoreEngine.EventBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreEngine.Hub
{
    /// <summary>
    /// Manager, Ui 등 단일객체 모듈을 위한 등록, 해제 이벤트
    /// </summary>
    public struct ModuleRegistrationEvent : IEvent, IRegistration
    {
        public IModule module;
        public bool isAdd { get; private set; } // 등록할 것인가, 해제할것인가
        public ContextScope scope { get; private set; } // 어느 Context 산하로 들어갈 것인가


        public ModuleRegistrationEvent(IModule module, bool isAdd, ContextScope scope)
        {
            this.module = module;
            this.isAdd = isAdd;
            this.scope = scope;
        }
    }

    internal abstract class BaseModuleHub<TModule> : BaseHub<ModuleRegistrationEvent>
        where TModule : class, IModule
    {
        // 단일 매니저들을 담아두는 딕셔너리
        protected Dictionary<Type, TModule> moduleDict = new();
        protected abstract bool moduleEnabled { get; } //Hub에서 초기화 후 module의 active 결정
        

        private bool _isInitStarted = false;
        private TModule[] _startInitModules;

        internal override void AwakeFromContext()
        {
            base.AwakeFromContext();

            // 모듈이 등록할 수 있도록 Context로부터 시작하는 가장 빠른 Awake 사용하여 구독
            EventBus<ModuleRegistrationEvent>.Subscribe(OnLeafRegistration);
        }

        internal override void OnDestroyFromContext()
        {
            base.OnDestroyFromContext();

            var modules = moduleDict.Values.ToArray();
            foreach (var module in modules)
            {
                if (!UtilitySystem.isUnityNull(module)) module.Exit();
            }
            // 모듈들정리를 끝낸 후 나도 구독취소
            // Hub가 어차피 사라질것이니 모듈들의 관리도 필요가 없어짐
            EventBus<ModuleRegistrationEvent>.Unsubscribe(OnLeafRegistration);

            // 게임이 종료 중이면 나머지 객체는 알아서 정리됨
            if (UtilitySystem.IsAppQuitting) return;

            foreach (var module in modules)
            {
                // 허브와 다른씬에 남아서 살아남을 수도 있으니
                if (!UtilitySystem.isUnityNull(module) && module is MonoBehaviour)
                {
                    MonoBehaviour asMono = module as MonoBehaviour;
                    Debug.Log($"Hub에서 module {asMono.name}을 수동 삭제함");
                    Destroy(asMono.gameObject);
                }
            }
        }

        public override IEnumerator Initialize()
        {
            yield return base.Initialize();

            // 초기화 열차는 이미 떠났음을 알림
            _isInitStarted = true;

            // IPriority를 상속받은 객체는 자신의 Priority 값으로 정렬하고,
            // 상속받지 않은 객체는 int.MaxValue를 부여하여 무조건 맨 뒤(후순위)로 밀어냄
            _startInitModules = moduleDict.Values
                .OrderBy(m => m is IPriority p ? p.Priority : int.MaxValue)
                .ToArray();

            foreach (var module in _startInitModules)
            {
                if (module != null)
                {
                    yield return module.Initialize();
                }
                yield return null; // 1프레임 대기 (화면 멈춤 방지)
            }
        }

        public override IEnumerator LateInitialize()
        {
            yield return base.LateInitialize();

            if(_startInitModules == null)
            {
                Debug.LogWarning($"{gameObject.name}의 {GetType().Name}에서 {nameof(LateInitialize)} 재시도");
                yield break;
            }

            foreach (var module in _startInitModules)
            {
                if(module is ILateInitialize lateModule)
                {
                    yield return lateModule?.LateInitialize();
                    yield return null;
                }
                // 상세 클래스에서 모듈 초기화 후 active를 결정
                module.SetActive(moduleEnabled);
            }

            // 다 썼으니 반환
            _startInitModules = null;
        }

        protected override void OnLeafRegistration(ModuleRegistrationEvent evt)
        {
            if (evt.module is TModule module)
            {
                base.OnLeafRegistration(evt);
            }
        }

        protected override void RegisterLeaf(ModuleRegistrationEvent evt)
        {
            TModule module = evt.module as TModule;

            // 등록한적이 없거나 등록했던 객체가 페이크 널일때
            Type typeKey = module.GetType();
            if (!moduleDict.TryGetValue(typeKey, out TModule old) || UtilitySystem.isUnityNull(old))
            {
                moduleDict[typeKey] = module;
            }

            // Hub의 직렬초기화 열차는 떠났으니 알아서 초기화 하렴
            if (_isInitStarted)
            {
                StartCoroutine(CatchUpRoutine(module));
            }
        }

        protected override void UnregisterLeaf(ModuleRegistrationEvent evt)
        {
            TModule module = evt.module as TModule;

            Type typeKey = module.GetType();
            if (moduleDict.ContainsKey(typeKey))
            {
                moduleDict.Remove(typeKey);
            }
        }

        private IEnumerator CatchUpRoutine(TModule tardyModule)
        {
            // 1. Initialize가 완전히 끝날 때까지 대기
            yield return tardyModule.Initialize();

            // 2. 이어서 LateInitialize 대기
            if (tardyModule is ILateInitialize lateModule)
            {
                yield return lateModule.LateInitialize();
            }

            // 3. 모든 초기화가 끝난 후 안전하게 Active 상태 세팅
            // (hardcoded true 대신 Hub의 정책인 moduleEnabled를 따르도록 수정!)
            tardyModule.SetActive(moduleEnabled);
        }

        // [수정된 부분] 
        // 1. T에 class 제약 조건을 추가하여 'as T' 캐스팅이 가능하게 만듭니다.
        // 2. 딕셔너리에 없을 경우를 대비한 return default(또는 null) 구문을 추가합니다.
        public virtual T GetModule<T>() where T : class, IModule // 또는 TModule
        {
            if (moduleDict.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }
            // 모듈을 찾지 못했을 때의 안전장치 및 경고
            Debug.LogWarning($"{gameObject.name}의 {GetType().Name}에 {typeof(T).Name} 모듈이 등록되어 있지 않습니다.");
            return null;
        }

    }
}

