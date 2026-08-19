//using System;
//using System.Collections.Generic;
//using Core.Network;
//using FishNet.Object;

//namespace Core.EventBus
//{
//    /// <summary>
//    /// FishNet 멀티플레이 환경에서 EventBus의 구독 및 해제를 수동/자동으로 관리하는 베이스 클래스
//    /// </summary>
//    public abstract class EventListenerNetworked : BaseLeafNetworked
//    {
//        // 💡 핵심: 등록된 델리게이트(Key)와 그에 매칭되는 static 해제 액션(Value)을 쌍으로 관리합니다.
//        private readonly Dictionary<Delegate, Action> _registeredEvents = new Dictionary<Delegate, Action>();

//        /// <summary>
//        /// 1. 특정 이벤트를 구독합니다. (중복 등록 방지 포함)
//        /// </summary>
//        protected void SubscribeTo<T>(Action<T> handler) where T : struct, IEvent
//        {
//            if (handler == null) return;

//            // 이미 등록된 핸들러라면 중복 등록하지 않음
//            if (!_registeredEvents.ContainsKey(handler))
//            {
//                EventBus<T>.Subscribe(handler);

//                // 해제할 때 호출할 static 제네릭 메서드를 익명 함수로 래핑하여 보관
//                _registeredEvents[handler] = () => EventBus<T>.Unsubscribe(handler);
//            }
//        }

//        /// <summary>
//        /// 2. 구독했던 특정 이벤트를 개별적으로 해제합니다.
//        /// </summary>
//        protected void UnsubscribeFrom<T>(Action<T> handler) where T : struct, IEvent
//        {
//            if (handler == null) return;

//            // 등록된 장바구니에서 해당 핸들러를 찾아 해제 액션을 실행
//            if (_registeredEvents.TryGetValue(handler, out Action unsubscribeAction))
//            {
//                unsubscribeAction?.Invoke();
//                _registeredEvents.Remove(handler);
//            }
//        }

//        /// <summary>
//        /// 3. 이 객체에 등록된 모든 이벤트를 한 번에 해제합니다.
//        /// </summary>
//        protected void UnsubscribeAll()
//        {
//            if (_registeredEvents.Count == 0) return;

//            // 보관 중인 모든 해제 액션을 순회하며 실행
//            foreach (var unsubscribeAction in _registeredEvents.Values)
//            {
//                unsubscribeAction?.Invoke();
//            }

//            _registeredEvents.Clear();
//        }



//        /// <summary>
//        /// 💡 멀티플레이 환경에서 메모리 누수(Static EventBus의 고질적 문제)를 막기 위한 최후의 안전장치
//        /// </summary>
//        protected override void OnDisable()
//        {
//            base.OnDisable();
//            // 개발자가 깜빡하고 해제하지 않았더라도, 객체가 비활성화되거나 풀에 반환될 때 완전히 청소합니다.
//            UnsubscribeAll();
//        }
//    }
//}