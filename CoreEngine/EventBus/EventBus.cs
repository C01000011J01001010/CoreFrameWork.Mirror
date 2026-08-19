using System;
using Unity.VisualScripting;

namespace CoreEngine.EventBus
{
    public interface IEvent { }

    // 💡 partial 키워드 추가
    public static partial class EventBus<T> where T : struct, IEvent
    {
        private static event Action<T> OnEvent;

        public static void Subscribe(Action<T> handler)
        {
            OnEvent -= handler;
            OnEvent += handler;
            LogSubscribe(handler); // 에디터에서만 작동, 빌드 시 증발
        }

        public static void Unsubscribe(Action<T> handler)
        {
            OnEvent -= handler;
            LogUnsubscribe(handler); // 에디터에서만 작동, 빌드 시 증발
        }

        public static void Publish(T eventData)
        {
            LogPublish(); // 에디터에서만 작동, 빌드 시 증발
            OnEvent?.Invoke(eventData);
        }

        public static void Clear()
        {
            OnEvent = null;
        }
    }
}