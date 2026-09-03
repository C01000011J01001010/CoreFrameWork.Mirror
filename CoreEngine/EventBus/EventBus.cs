using System;
using System.Collections.Generic;

namespace CoreEngine.EventBus
{
    public interface IEvent { }

    public static partial class EventBus<T> where T : struct, IEvent
    {
        struct Request
        {
            public Action<T> OnEvent;
            public bool IsAdd;
            public Request(Action<T> onEvent, bool isAdd)
            {
                OnEvent = onEvent;
                IsAdd = isAdd;
            }
        }

        readonly static HashSet<Action<T>> OnEventSet = new();
        readonly static List<Request> LateRequests = new();
        static bool isPublishing = false;


        public static void Subscribe(Action<T> handler)
        {
            if(isPublishing)
            {
                LateRequests.Add(new Request(handler, true));
            }
            else
            {
                // 중복은 알아서 걸러질테니 일단 넣기
                OnEventSet.Add(handler);
                LogSubscribe(handler);
            }
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (isPublishing)
            {
                LateRequests.Add(new Request(handler, false));
            }
            else
            {
                OnEventSet.Remove(handler);
                LogUnsubscribe(handler);
            }
            
        }

        public static void Publish(T eventData)
        {
            if (OnEventSet.Count == 0) return;

            // Event를 전달하는 중에 등록 취소를 방지
            isPublishing = true;
            try
            {
                LogPublish();
                foreach (var func in OnEventSet)
                {
                    try
                    {
                        func(eventData);
                    }
                    catch (Exception ex)
                    {
                        // 에러를 발생시킨 정확한 객체와 함수명 추출
                        string targetName = func.Target != null ? func.Target.GetType().Name : "Static Method";
                        string methodName = func.Method.Name;

                        UnityEngine.Debug.LogError(
                            $"[EventBus Error] '{typeof(T).Name}' 이벤트 처리 중 에러 발생!\n" +
                            $"범인 객체: {targetName}\n" +
                            $"범인 함수: {methodName}\n" +
                            $"에러 내용: {ex.Message}"
                        );
                    }
                }
            }
            finally
            {
                // 콜백 내부 에러 발생 시에도 락(isPublishing)이 확실히 풀리도록 방어
                isPublishing = false;
            }

            // 이벤트를 전달하는 중 등록 요청이 있었다면 순서대로 처리
            if (LateRequests.Count == 0) return;

            foreach(var requst in LateRequests)
            {
                if(requst.IsAdd)
                {
                    OnEventSet.Add(requst.OnEvent);
                    LogSubscribe(requst.OnEvent);
                }
                else
                {
                    OnEventSet.Remove(requst.OnEvent);
                    LogUnsubscribe(requst.OnEvent);
                }
            }
            LateRequests.Clear();
        }

        public static void Clear()
        {
            OnEventSet.Clear();
        }
    }
}