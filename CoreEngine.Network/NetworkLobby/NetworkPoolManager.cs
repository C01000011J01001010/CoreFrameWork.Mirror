using CoreEngine.Manager;

namespace CoreEngine.Network.Pool
{
    public enum NetworkPoolType
    {
        LobbyClientBox,
        // 필요시 더 추가
    }

    /// <summary>
    /// 네트워크 관련 UI나 객체들을 풀링으로 관리하는 전담 매니저
    /// </summary>
    public class NetworkPoolManager : ObjectPoolManager<NetworkPoolType>
    {

    }
}