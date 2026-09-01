using System;
using CoreEngine.Interface;
namespace CoreEngine.EventBus
{
    /// <summary>
    /// <para>이벤트를 구독하고, 이벤트가 발행되면 전달받은 함수를 실행하는 클래스</para>
    /// <para></para>
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class RepeatEventConsumer<TEvent> : IBindable where TEvent : struct, IEvent
    {
        private readonly Action<TEvent> _onEventPublished;

        // 생성자: 응답(Pong)을 처리할 함수를 전달받음
        public RepeatEventConsumer(Action<TEvent> onEventPublished)
        {
            _onEventPublished = onEventPublished;
        }

        /// <summary>
        /// 이벤트를 구독하고, 이벤트 발행자가 있는지 요청함
        /// </summary>
        public void Bind()
        {
            // 응답을 들을 귀를 먼저 열고
            EventBus<TEvent>.Unsubscribe(_onEventPublished); // 중복구독 방지
            EventBus<TEvent>.Subscribe(_onEventPublished);

            // 대상이 있는지 이벤트 요청 발송
            EventBus<Repeat<TEvent>>.Publish(new Repeat<TEvent>());
        }

        public void Unbind()
        {
            EventBus<TEvent>.Unsubscribe(_onEventPublished);
        }
    }
}