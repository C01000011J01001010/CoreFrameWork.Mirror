//using FishNet.Object;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Core.EventBus
//{
//    /// <summary>
//    /// 수동 해제로직 EventListener
//    /// <para>GC부담이 전혀없음</para>
//    /// <para>자주 꺼졌다 켜지는 객체에 사용</para>
//    /// <para>ex)총알, 이펙트 등 1초에 수십번 On,Off 하는 객체</para>
//    /// </summary>
//    public abstract class NetWorkEventListener_Manual : NetworkBehaviour
//    {
//        private HashSet<object> SubscribedHandlers = new();

//        // 자식 클래스에서 이벤트를 등록할 때 호출하는 함수
//        protected void SubscribeTo<T>(Action<T> handler) where T : struct, IEvent
//        {
//            if (SubscribedHandlers.Add(handler))
//            {
//                EventBus<T>.Subscribe(handler);
//            }
//        }

//        // 자식 클래스에서 이벤트를 해제할 때 호출하는 함수
//        protected void UnsubscribeFrom<T>(Action<T> handler) where T : struct, IEvent
//        {
//            if (SubscribedHandlers.Remove(handler))
//            {
//                EventBus<T>.Unsubscribe(handler);
//            }
//        }

//        // 자식 클래스에게 강제하는 추상 메서드 (개발자가 무조건 구현해야 함)
//        protected abstract void RegisterEvents();
//        protected abstract void UnregisterEvents();

//        private void OnEnable()
//        {
//            RegisterEvents();
//        }

//        private void OnDisable()
//        {
//            UnregisterEvents();
//        }
//    }
//}

