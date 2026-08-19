#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreEngine.EventBus
{
    public interface IEventBusControl
    {
        string EventTypeName { get; }
        bool DebugLogEnabled { get; set; }
        int SubscriberCount { get; }
        void ClearBus();
    }

    public static class EventBusRegistry
    {
        public static readonly List<IEventBusControl> ActiveBuses = new();
        public static bool MasterDebugLog = false;

        // 💡 팁: 유니티 에디터에서 Play 버튼을 누를 때마다 리스트를 초기화해줍니다.
        // (에디터 환경에서 플레이/정지를 반복할 때 리스트가 무한 증식하는 것을 방지)
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearRegistry()
        {
            ActiveBuses.Clear();
        }
    }

    // 💡 EventBus.cs의 나머지 절반을 여기서 구현합니다.
    public static partial class EventBus<T>
    {
        private static bool _enableDebugLog = false;
        private static readonly ControlInstance Controller = new();

        static EventBus()
        {
            EventBusRegistry.ActiveBuses.Add(Controller);
        }

        public static bool EnableDebugLog
        {
            get => EventBusRegistry.MasterDebugLog || _enableDebugLog;
            set => _enableDebugLog = value;
        }

        private class ControlInstance : IEventBusControl
        {
            public string EventTypeName => typeof(T).Name;
            public bool DebugLogEnabled { get => _enableDebugLog; set => _enableDebugLog = value; }
            public int SubscriberCount => OnEvent?.GetInvocationList().Length ?? 0;
            public void ClearBus() => Clear();
        }

        // =================================================================
        // 실제 로그를 출력하는 Conditional 메서드들 (이전 코드에서 누락된 부분)
        // =================================================================
        [Conditional("UNITY_EDITOR")]
        private static void LogPublish()
        {
            if (!EnableDebugLog) return;
            var frame = new StackTrace().GetFrame(2);
            string caller = frame != null ? frame.GetMethod().DeclaringType.Name : "Unknown";
            UnityEngine.Debug.Log($"<color=#00FF00>[EventBus]</color> <b>{caller}</b> ➔ Publish: <color=#FFFF00><b>{typeof(T).Name}</b></color> (수신 대기: {Controller.SubscriberCount}명)");
        }

        [Conditional("UNITY_EDITOR")]
        private static void LogSubscribe(Action<T> handler)
        {
            if (!EnableDebugLog) return;
            UnityEngine.Debug.Log($"<color=#00FF00>[EventBus]</color> Subscribe: <b>{typeof(T).Name}</b> (by {handler.Target?.GetType().Name})");
        }

        [Conditional("UNITY_EDITOR")]
        private static void LogUnsubscribe(Action<T> handler)
        {
            if (!EnableDebugLog) return;
            UnityEngine.Debug.Log($"<color=#00FF00>[EventBus]</color> Unsubscribe: <b>{typeof(T).Name}</b> (by {handler.Target?.GetType().Name})");
        }
    }
}
#endif