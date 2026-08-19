using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위한 네임스페이스
using UnityEngine.UI;
using FishNet;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace CoreEngine.Network
{
    public class NetworkTestUI : BaseUi, IUi
    {
        [Header("--- Common Settings ---")]
        [Tooltip("호스트, 서버, 클라이언트가 공통으로 사용할 포트 입력칸")]
        [SerializeField] private TMP_InputField portInputField;

        [Header("--- Client Settings ---")]
        [Tooltip("클라이언트가 접속할 때 입력할 호스트의 IP 주소 입력칸")]
        [SerializeField] private TMP_InputField clientIpInputField;

        [Header("--- Host/Server Info ---")]
        [Tooltip("방을 팠을 때 친구에게 알려줄 내 IP가 표시될 텍스트")]
        [SerializeField] private TextMeshProUGUI myIpDisplayText;
        [Tooltip("클릭 시 IP를 복사할 버튼")]
        [SerializeField] private Button copyIpButton;

        [Header("--- Buttons ---")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button serverButton;
        [SerializeField] private Button clientButton;

        protected override void Awake()
        {
            // 허브에 등록 안하니 주석처리
            //base.Awake();


            // 1. 기본값 세팅 (입력칸이 비어있을 때를 대비)
            portInputField.text = "7770";      // FishNet 기본 포트
            clientIpInputField.text = "localhost"; // 기본 클라이언트 접속처 (자기 자신)
            myIpDisplayText.text = "IP: 대기 중...";

            // 2. 버튼 클릭 이벤트 연결 (인스펙터에서 드래그 안 하고 코드로 직접 연결)
            hostButton.onClick.AddListener(StartHost);
            serverButton.onClick.AddListener(StartServer);
            clientButton.onClick.AddListener(StartClient);
            copyIpButton.onClick.AddListener(CopyIpToClipboard);
        }

        private void StartHost()
        {
            ushort port = GetPort();

            // 💡 수정: 통신 배달부(Tugboat)의 설정값을 직접 변경하여 호환성을 높임
            if (InstanceFinder.TransportManager.Transport is FishNet.Transporting.Tugboat.Tugboat tugboat)
            {
                tugboat.SetPort(port);
                tugboat.SetClientAddress("localhost");
            }

            // 오버로딩 없이 기본 호출을 사용하여 안전하게 연결
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();

            // 💡 친구를 위해 내 외부 IP를 불러와서 화면에 띄웁니다.
            ShowMyPublicIpAsync();
            DisableButtons(); // 중복 접속 방지를 위해 버튼 비활성화
        }

        private void StartServer()
        {
            ushort port = GetPort();

            if (InstanceFinder.TransportManager.Transport is FishNet.Transporting.Tugboat.Tugboat tugboat)
            {
                tugboat.SetPort(port);
            }

            InstanceFinder.ServerManager.StartConnection();

            ShowMyPublicIpAsync();
            DisableButtons();
        }

        private void StartClient()
        {
            string ip = clientIpInputField.text;
            if (string.IsNullOrEmpty(ip)) ip = "localhost";
            ushort port = GetPort();

            if (InstanceFinder.TransportManager.Transport is FishNet.Transporting.Tugboat.Tugboat tugboat)
            {
                tugboat.SetPort(port);
                tugboat.SetClientAddress(ip);
            }

            InstanceFinder.ClientManager.StartConnection();
            DisableButtons();
        }

        // 포트 입력칸의 텍스트를 숫자로 안전하게 변환하는 헬퍼 함수
        private ushort GetPort()
        {
            if (ushort.TryParse(portInputField.text, out ushort port))
            {
                return port;
            }
            return 7770; // 파싱 실패 시 기본 포트 7770 반환
        }

        // 외부 API를 통해 내 공인 IP를 비동기로 가져와서 텍스트에 띄워주는 함수
        private async void ShowMyPublicIpAsync()
        {
            myIpDisplayText.text = "IP 불러오는 중...";
            string publicIp = await FetchPublicIpTask();
            myIpDisplayText.text = publicIp;
        }

        // 화면에 띄워진 IP를 클립보드에 복사하는 함수
        private void CopyIpToClipboard()
        {
            string ipText = myIpDisplayText.text;

            // 로딩 중이거나 에러 상태가 아닐 때만 복사 수행
            if (!ipText.Contains("불러오는 중") && !ipText.Contains("실패"))
            {
                // 유니티 내장 기능: 클립보드에 텍스트 복사
                GUIUtility.systemCopyBuffer = ipText;
                Debug.Log($"[클립보드 복사 완료] 친구에게 이 주소를 알려주세요: {ipText}");
            }
        }

        // IP를 긁어오는 코어 비동기 로직
        private async Task<string> FetchPublicIpTask()
        {
            using (UnityWebRequest request = UnityWebRequest.Get("https://api.ipify.org"))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text; // 예: "121.254.123.45"
                }
                else
                {
                    Debug.LogError("IP를 가져오는데 실패했습니다: " + request.error);
                    return "IP 로드 실패";
                }
            }
        }

        // 접속이 시작되면 버튼들을 꺼서 여러 번 눌리는 것을 방지하는 함수
        private void DisableButtons()
        {
            hostButton.interactable = false;
            serverButton.interactable = false;
            clientButton.interactable = false;
        }
    }
}
