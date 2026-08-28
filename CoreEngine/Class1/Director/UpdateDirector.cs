using CoreEngine.EventBus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine
{
    #region Tcik구별을 위한 enum
    // None을 제외한 enum 순서를 Update 순서에 사용
    public enum TickGroup
    {
        None,
        Initial,
        Controller,
        Character,
        Object,
        Ui,
    }
    public enum LateTickGroup
    {
        None,
        Camera,
        Ui,
        Post,
    }
    public enum FixedTickGroup
    {
        None,
        Physics
    }
    #endregion

    #region Tick 인터페이스
    public interface ITickable
    {
        TickGroup TickGroup { get; }
        void Tick(float deltaTime);
    }

    public interface ILateTickable
    {
        LateTickGroup LateTickGroup { get; }
        void LateTick(float dt);
    }

    public interface IFixedTickable
    {
        FixedTickGroup FixedTickGroup { get; }
        void FixedTick(float fixedDeltaTime);
    }
    #endregion

    #region Tick 등록용 이벤트
    public struct R_TickEvent : IEvent
    {
        public ITickable Target;
        /// <summary>
        /// 자신의 업데이트 순서 결정
        /// </summary>
        public TickGroup Group;
        /// <summary>
        /// true : 구독추가
        /// <para>false: 구독취소</para> 
        /// </summary>
        public bool IsAdd;
        public R_TickEvent(ITickable target, TickGroup group, bool isAdd) { Target = target; Group = group; IsAdd = isAdd; }
    }
    public struct R_LateTickEvent : IEvent
    {
        public ILateTickable Target;
        /// <summary>
        /// 자신의 업데이트 순서 결정
        /// </summary>
        public LateTickGroup Group;
        /// <summary>
        /// true : 구독추가
        /// <para>false: 구독취소</para> 
        /// </summary>
        public bool IsAdd;
        public R_LateTickEvent(ILateTickable target, LateTickGroup group, bool isAdd) { Target = target; Group = group; IsAdd = isAdd; }
    }
    public struct R_FixedTickEvent : IEvent
    {
        public IFixedTickable Target;
        /// <summary>
        /// 자신의 업데이트 순서 결정
        /// </summary>
        public FixedTickGroup Group;
        /// <summary>
        /// true : 구독추가
        /// <para>false: 구독취소</para> 
        /// </summary>
        public bool IsAdd;
        public R_FixedTickEvent(IFixedTickable target, FixedTickGroup group, bool isAdd) { Target = target; Group = group; IsAdd = isAdd; }
    }
    #endregion
}

namespace CoreEngine.Director
{
    [DefaultExecutionOrder((int)ExecutionOrder.UpdateDirector)]
    internal sealed class UpdateDirector : BaseDirector<UpdateDirector>
    {
        #region Runner
        private interface IRunner { void Run(float dt); }
        private abstract class BaseRunner<TInterface> : IRunner
        {
            protected readonly HashSet<TInterface> _active = new HashSet<TInterface>(100);
            private readonly List<TInterface> _pendingAdds = new List<TInterface>(20);
            private readonly List<TInterface> _pendingRemoves = new List<TInterface>(20);
            private bool _isUpdating = false;

            public void Register(TInterface target, bool isAdd)
            {
                // 업데이트 중이면 예약 걸어놓기
                if (_isUpdating)
                {
                    if (isAdd) _pendingAdds.Add(target);
                    else _pendingRemoves.Add(target);
                }
                else
                {
                    if (isAdd) _active.Add(target);
                    else _active.Remove(target);
                }
            }

            protected abstract void Execute(float dt);

            protected bool TryHandleInvalidTarget(TInterface target)
            {
                if (target is UnityEngine.Object unityObj && unityObj == null)
                {
                    // 💥 개발 중에 발생하면 반드시 고쳐야 할 버그입니다!
                    UtilityLog.Log($"[UpdateManager] 시스템 오류: 틱 리스트에 Destroy된 객체가 남아있습니다.", LogColor.Red);
                    _pendingRemoves.Add(target);
                    return true;
                }
                // 2. 일반 C# 객체인 경우
                else if (target == null)
                {
                    UtilityLog.Log($"[UpdateManager] 시스템 오류: 틱 리스트에 Null 객체가 있습니다.", LogColor.Red);
                    _pendingRemoves.Add(target);
                    return true;
                }
                return false;
            }

            public void Run(float dt)
            {
                _isUpdating = true;
                //foreach (var target in _active)
                //{
                //    if (target != null) target.Tick(dt); // 안전망
                //}
                Execute(dt);
                _isUpdating = false;

                // 밀린 작업 일괄 처리 (GC 발생 안함)
                if (_pendingAdds.Count > 0) { foreach (var t in _pendingAdds) _active.Add(t); _pendingAdds.Clear(); }
                if (_pendingRemoves.Count > 0) { foreach (var t in _pendingRemoves) _active.Remove(t); _pendingRemoves.Clear(); }
            }
        }
        private class TickRunner : BaseRunner<ITickable>
        {
            protected override void Execute(float dt)
            {
                foreach (var target in _active)
                {
                    if(!TryHandleInvalidTarget(target))
                        target.Tick(dt);
                }
            }
        }
        private class FixedTickRunner : BaseRunner<IFixedTickable>
        {
            protected override void Execute(float dt)
            {
                foreach (var target in _active)
                {
                    if (!TryHandleInvalidTarget(target))
                        target.FixedTick(dt);
                }
            }
        }
        private class LateTickRunner : BaseRunner<ILateTickable>
        {
            protected override void Execute(float dt)
            {
                foreach (var target in _active)
                {
                    if (!TryHandleInvalidTarget(target)) 
                        target.LateTick(dt);
                }
            }
        }
        #endregion

        // 💡 핵심 2: Dictionary 대신 Enum을 정수(int)로 변환해 배열 인덱스로 사용 (압도적인 성능)
        private TickRunner[] _tickRunners;
        private LateTickRunner[] _lateTickRunners;
        private FixedTickRunner[] _fixedTickRunners;

        // 글로벌 틱 가동 여부 플래그
        private bool _isTickingAllowed = false;

        // SceneContext가 초기화를 완료한 후 호출할 진입점
        internal static void StartTicking()
        {
            if (Inst == null) return;
            Inst._isTickingAllowed = true;
            UtilityLog.Log("[UpdateManager] 업데이트 루프가 가동되었습니다.", LogColor.Green);
        }

        internal static void StopTicking()
        {
            if (Inst == null) return;
            Inst._isTickingAllowed = false;
            UtilityLog.Log("[UpdateManager] 업데이트 루프가 멈췄습니다.", LogColor.Green);
        }

        private void OnEnable()
        {
            EventBus<R_TickEvent>.Subscribe(OnRegisterTick);
            EventBus<R_LateTickEvent>.Subscribe(OnRegisterLateTick);
            EventBus<R_FixedTickEvent>.Subscribe(OnRegisterFixedTick);
        }

        private void OnDisable()
        {
            EventBus<R_TickEvent>.Unsubscribe(OnRegisterTick);
            EventBus<R_LateTickEvent>.Unsubscribe(OnRegisterLateTick);
            EventBus<R_FixedTickEvent>.Unsubscribe(OnRegisterFixedTick);
        }

        protected override void Awake()
        {
             base.Awake();
            _tickRunners = CreateRunners<TickGroup, TickRunner>();
            _lateTickRunners = CreateRunners<LateTickGroup, LateTickRunner>();
            _fixedTickRunners = CreateRunners<FixedTickGroup, FixedTickRunner>();
        }

        private TRunner[] CreateRunners<TGroup, TRunner>()
            where TGroup : struct, Enum
            where TRunner : IRunner, new()
        {
            int groupCount = Enum.GetValues(typeof(TGroup)).Length;

            TRunner[] runners = new TRunner[groupCount];

            // None을 제외한 각 러너 생성
            // None은 배열의 한 공간은 차지하지만 실제 객체를 만들지는 않음
            for (int i = 1; i < groupCount; i++) runners[i] = new TRunner();

            return runners;
        }

        // 이벤트 수신 시 해당 그룹의 인덱스를 찾아 Runner에게 위임
        private void OnRegisterTick(R_TickEvent evt)
        {
            // None(0)이면 배열에 접근하기 전에 입구 컷!
            if (evt.Group == TickGroup.None) return;

            _tickRunners[(int)evt.Group].Register(evt.Target, evt.IsAdd);
        }

        private void OnRegisterLateTick(R_LateTickEvent evt)
        {
            if (evt.Group == LateTickGroup.None) return;

            _lateTickRunners[(int)evt.Group].Register(evt.Target, evt.IsAdd);
        }

        private void OnRegisterFixedTick(R_FixedTickEvent evt)
        {
            if (evt.Group == FixedTickGroup.None) return;

            _fixedTickRunners[(int)evt.Group].Register(evt.Target, evt.IsAdd);
        }

        // ==========================================
        // 유니티 생명주기 제어부 (순서 보장)
        // ==========================================

        private void Update()
        {
            if(_isTickingAllowed)
                Run(_tickRunners, Time.deltaTime);
        }
        private void LateUpdate()
        {
            if (_isTickingAllowed)
                Run(_lateTickRunners, Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_isTickingAllowed)
                Run(_fixedTickRunners, Time.fixedDeltaTime);
        }

        private void Run(IRunner[] runner, float dt)
        {
            // Runner가 생성되지 않은 None을 제외하고 순회
            for (int i = 1; i < runner.Length; i++)
            {
                runner[i].Run(dt);
            }
        }
    }
}