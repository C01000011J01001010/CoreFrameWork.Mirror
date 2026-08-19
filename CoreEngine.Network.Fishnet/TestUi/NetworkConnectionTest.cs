using CoreEngine.EventBus;
using FishNet;
using UnityEngine;

namespace CoreEngine.Network
{
    public class NetworkConnectionTest : MonoBehaviour
    {
        private void OnGUI()
        {
            // 💡 수정된 부분: IsServerStarted와 IsClientStarted 사용
            if (InstanceFinder.IsServerStarted || InstanceFinder.IsClientStarted) return;

            if (GUI.Button(new Rect(10, 10, 150, 50), "방장으로 시작 (Host)"))
            {
                // 서버와 로컬 클라이언트를 동시에 켭니다.
                InstanceFinder.ServerManager.StartConnection();
                InstanceFinder.ClientManager.StartConnection();

                EventBus<ServerStartEvent>.Publish(new ServerStartEvent());
            }

            if (GUI.Button(new Rect(10, 70, 150, 50), "손님으로 접속 (Client)"))
            {
                // 클라이언트로만 접속합니다.
                InstanceFinder.ClientManager.StartConnection();
            }
        }
    }
}
