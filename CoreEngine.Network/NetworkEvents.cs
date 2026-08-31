
using CoreEngine.EventBus;

namespace CoreEngine.Network
{
    public enum ConnectionMode { Host, Server, Client }

    // 접속 요청 이벤트
    public struct ConnectRequestEvent : IEvent
    {
        public ConnectionMode Mode;
        public string IpAddress;
        public ushort Port;
    }

    // 접속 성공 이벤트 
    public struct NetworkConnectionSuccessEvent : IEvent { }

    // 접속 실패 이벤트
    public struct NetworkConnectionFailEvent : IEvent
    {
        public string ErrorMessage;
    }

    // 네트워크 연결 해제 이벤트
    public struct NetworkConnectionLostEvent : IEvent { }
}