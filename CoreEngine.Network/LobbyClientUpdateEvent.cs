using CoreEngine.EventBus;

namespace CoreEngine.Network.Lobby
{
    public enum ClientUpdate
    {
        Add, Remove, Clear
    }
    // 대기실 유저 목록이 변경되었을 때 UI에 뿌려줄 이벤트
    public struct LobbyClientUpdateEvent : IEvent
    {
        public int ClientId;
        public string IpAddress;
        public ClientUpdate clientUpdate;
    }
}