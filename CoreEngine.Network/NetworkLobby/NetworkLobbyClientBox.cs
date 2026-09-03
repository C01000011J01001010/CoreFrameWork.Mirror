using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using CoreEngine.Manager.Pool;

namespace CoreEngine.Network.Lobby.Ui
{
    // IPoolable을 상속받아 PoolHandler의 생명주기 통제를 받습니다.
    public class NetworkLobbyClientBox : MonoBehaviour, IPoolable
    {
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private TextMeshProUGUI ipText;
        [SerializeField] private Image backgroundImage;

        private Color _originalColor;

        // IPoolable 규약: Unity 내장 IObjectPool 참조
        public IPoolReleaser Releaser { get; set; }

        private void Awake()
        {
            _originalColor = backgroundImage.color;
        }

        public void Setup(int clientId, string ip, bool isLocal, Color localColor)
        {
            idText.text = $"Client ID: {clientId}";

            // UI 객체 스스로가 내 것(isLocal)인지 판단하여 텍스트를 가공
            if (isLocal)
            {
                ipText.text = $"IP: {ip} (Me)";
                backgroundImage.color = localColor;
            }
            else
            {
                ipText.text = $"IP: {ip}";
                backgroundImage.color = _originalColor;
            }
        }

        // --- IPoolable 생명주기 구현 ---
        public void OnSpawn()
        {
            // 꺼내질 때 필요한 초기화가 있다면 여기서 처리
        }

        public void OnDespawn()
        {
            // 반환될 때 텍스트나 색상을 초기화하여 메모리 찌꺼기를 지웁니다
            idText.text = "";
            ipText.text = "";
            backgroundImage.color = _originalColor;
        }

        // 외부(UI 컨트롤러)에서 파괴 대신 호출할 함수
        public void ReturnToPool()
        {
            // Manager를 거치지 않고 직접 반환
            if (Releaser != null)
            {
                Releaser.Release(this);
            }
        }
    }
}