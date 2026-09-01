using System;
using CoreEngine.Interface;
namespace CoreEngine.EventBus
{
    public struct Repeat<T> : IEvent where T : IEvent { }

    /// <summary>
    /// 반복해서 발행할 이벤트를 생성하는 함수를 전달받아, 요청(Ping)이 들어오면 해당 함수를 사용해 이벤트를 발행(Pong)하는 클래스
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class RepeatEventProvider<TEvent> : IBindable where TEvent : struct, IEvent
    {
        private readonly Func<TEvent> _evnetCreatFunc;

        // 생성자: 요청(Ping)이 왔을 때 반복 발행할 데이터(Pong) 생성 함수를 전달받음
        public RepeatEventProvider(Func<TEvent> evnetCreatFunc)
        {
            _evnetCreatFunc = evnetCreatFunc;
        }

        /// <summary>
        /// 이벤트를 발행하고, 나중에 요청(Ping)이 들어오면 반복 발행(Pong)할 수 있도록 귀를 열어둠
        /// </summary>
        public void Bind()
        {
            // 일반 이벤트 발행하고
            EventBus<TEvent>.Publish(_evnetCreatFunc());

            // 나중에 필요한 경우를 위해 반복 발행 요청(Ping)을 들을 귀를 열어둠
            EventBus<Repeat<TEvent>>.Unsubscribe(OnRequestRepeat); // 중복 구독 방지
            EventBus<Repeat<TEvent>>.Subscribe(OnRequestRepeat);
        }

        public void Unbind()
        {
            EventBus<Repeat<TEvent>>.Unsubscribe(OnRequestRepeat);
        }

        private void OnRequestRepeat(Repeat<TEvent> evt)
        {
            // 누군가 요청(Ping)을 던지면 함수를 사용해 반복 발행
            EventBus<TEvent>.Publish(_evnetCreatFunc());
        }
    }
}