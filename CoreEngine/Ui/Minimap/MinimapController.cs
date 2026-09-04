using CoreEngine.Interface;
using CoreEngine.Helpers;
using CoreEngine.LevelDesign;
using CoreEngine.Manager; // 범용 프레임워크인 ResourceManager 호출용
using UnityEngine;
using UnityEngine.UI;
using CoreEngine.Facades;

namespace CoreEngine.UI
{
    /// <summary>
    /// 화면 회전 없이, 플레이어의 월드 좌표만 추적하여 3x3 그리드 타일을 
    /// 비동기(Addressables)로 교체하는 고정형 미니맵 UI
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup), typeof(RectTransform))]
    public class MinimapController : CoreMonoBehaviour, ILateTickable
    {
        [Header("Map Data")]
        [SerializeField] private MapGridDataSO _mapData;

        [Header("UI Hierarchy")]
        [Tooltip("가로(Width)는 고정하고, 맵 비율에 맞춰 세로(Height)를 조절할 부모 캔버스")]
        [SerializeField] private RectTransform _heightSetTarget;

        [Header("3x3 Tile RawImages (Top-Left to Bottom-Right)")]
        [SerializeField] private RawImage[] _tileImages = new RawImage[9];

        [Header("UI Tile Config")]
        [SerializeField] private Vector2 _uiTileSize = new Vector2(200f, 200f);

        // 다중 상속 우회용 수신기 (Composition)
        private readonly InterfaceReceiver<IMapTargetProvider> _receiver = new();

        private RectTransform _rectTransform;
        private Vector2Int _currentCenterGridIndex;

        // ✨ 핵심: 각 UI 타일(0~8)이 현재 '어떤 주소'의 이미지를 로드/표시 중인지 추적
        // (네트워크 지연이나 빠른 이동 시 엉뚱한 이미지가 덮어씌워지는 Race Condition 방지용)
        private string[] _currentTileAddresses = new string[9];

        public LateTickGroup LateTickGroup => LateTickGroup.Ui;

        private ResourceManager _resourceManager;
        private ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager != null) return _resourceManager;
                _resourceManager = CoreFacade.GetManager<ResourceManager>();
                return _resourceManager;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _receiver.Bind();
            _currentCenterGridIndex = new Vector2Int(-1, -1);

            // UI 켜질 때 주소 추적 배열 초기화
            for (int i = 0; i < 9; i++)
            {
                _currentTileAddresses[i] = string.Empty;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _receiver.Unbind();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // 자식 RawImage 9개 자동 할당 (null 예방)
            if (_tileImages == null || _tileImages.Length < 9) _tileImages = new RawImage[9];
            var tiles = GetComponentsInChildren<RawImage>();
            for (int i = 0; i < _tileImages.Length && i < tiles.Length; i++)
            {
                if (_tileImages[i] == null && tiles[i] != null)
                {
                    _tileImages[i] = tiles[i];
                }
            }
        }

        private void Start()
        {
            AdjustDrawCanvasHeight();
        }

        public void LateTick(float dt)
        {
            if (!_receiver.TryGet(out var targetProvider) || _mapData == null) return;

            Vector3 worldPos = targetProvider.WorldPosition;

            UpdateMapGridTiles(worldPos);
            UpdateMapPosition(worldPos);
        }

        // =========================================================
        // 🌟 맵 비율 동기화 로직
        // =========================================================
        private void AdjustDrawCanvasHeight()
        {
            if (_mapData == null || _heightSetTarget == null) return;

            float ratio = _mapData.tileSize.y / _mapData.tileSize.x;
            float currentWidth = _heightSetTarget.rect.width;

            _heightSetTarget.sizeDelta = new Vector2(_heightSetTarget.sizeDelta.x, currentWidth * ratio);
            _uiTileSize = new Vector2(_rectTransform.rect.width, _rectTransform.rect.height);

            var sizeFitter = GetComponentInParent<MinimapSizeFitter>();
            if (sizeFitter != null) sizeFitter.AdjustHeight();
            else LogHelper.Log("[MinimapController] 부모 구조에서 MinimapSizeFitter를 찾을 수 없습니다.", LogColor.Yellow);
        }


        // =========================================================
        // 🌟 타일 교체 및 비동기 로드(ResourceManager 호출) 로직
        // =========================================================
        private void UpdateMapGridTiles(Vector3 worldPos)
        {
            // 현재 캐릭터가 밟고 있는 중심 타일의 Grid 좌표 계산
            Vector2Int centerGrid = _mapData.GetGridIndex(worldPos);
            if (centerGrid == _currentCenterGridIndex) return; // 격자가 바뀌지 않았다면 로드 연산 스킵

            _currentCenterGridIndex = centerGrid;

            int tileIndex = 0;
            for (int rowOffset = 1; rowOffset >= -1; rowOffset--)
            {
                for (int colOffset = -1; colOffset <= 1; colOffset++)
                {
                    int targetCol = centerGrid.x + colOffset;
                    int targetRow = centerGrid.y + rowOffset;

                    bool isValidTile = targetCol >= 0 && targetCol < _mapData.totalCols &&
                                       targetRow >= 0 && targetRow < _mapData.totalRows;

                    if (isValidTile && tileIndex < _tileImages.Length && _tileImages[tileIndex] != null)
                    {
                        // 1. 유저님의 네이밍 규칙에 맞춘 Addressable 주소(Key) 조합
                        // 예: "Tile_0_0" 
                        string targetAddress = $"{_mapData.saveDirectory}/Tile_{targetCol}_{targetRow}.png";

                        // 2. 이미 이 타일 칸이 해당 주소의 이미지를 요청했거나 띄우고 있다면 스킵 (중복 로드 방지)
                        if (_currentTileAddresses[tileIndex] != targetAddress)
                        {
                            _currentTileAddresses[tileIndex] = targetAddress; // 현재 처리 중인 주소 기록

                            // 새 이미지가 로드되기 전까지 이전 이미지가 보이지 않도록 투명 처리
                            _tileImages[tileIndex].texture = null;
                            _tileImages[tileIndex].enabled = false;

                            // 람다식 콜백에서 tileIndex 변수가 꼬이지 않도록 안전하게 캡처(Closure 방어)
                            int captureIndex = tileIndex;

                            // 3. ✨ 범용 프레임워크인 ResourceManager에게 로드 및 캐싱 위임
                            // 컨트롤러는 내부가 Addressables인지, 로컬 파일인지 몰라도 됨!
                            ResourceManager.LoadSceneAssetAsync<Texture2D>(targetAddress, (loadedTex) =>
                            {
                                // 🌟 Race Condition 방어 🌟
                                // 로딩에 0.5초가 걸렸는데, 그 사이 플레이어가 엄청 빨리 이동해서
                                // 이 칸에 "Tile_1_1"이 아니라 "Tile_1_2"가 떠야 하는 상황으로 바뀌었다면?
                                // 현재 띄워야 할 주소(_currentTileAddresses)와 가져온 주소가 일치할 때만 띄움!
                                if (_currentTileAddresses[captureIndex] == targetAddress)
                                {
                                    _tileImages[captureIndex].texture = loadedTex;
                                    _tileImages[captureIndex].enabled = true;
                                }
                            });
                        }
                    }
                    else if (tileIndex < _tileImages.Length && _tileImages[tileIndex] != null)
                    {
                        // 맵 범위를 벗어난 허공(바다 등)일 경우 타일을 아예 끔
                        _currentTileAddresses[tileIndex] = string.Empty;
                        _tileImages[tileIndex].enabled = false;
                        _tileImages[tileIndex].texture = null;
                    }

                    tileIndex++;
                }
            }
        }

        // =========================================================
        // 🌟 타일 위치(UI 앵커 좌표) 보정 로직
        // =========================================================
        private void UpdateMapPosition(Vector3 worldPos)
        {
            if (_currentCenterGridIndex.x < 0) return;

            // 중앙 타일의 3D 월드 중심 좌표 산출
            Vector2 centerTileWorldCenter = _mapData.worldMinBounds + new Vector2(
                (_currentCenterGridIndex.x + 0.5f) * _mapData.tileSize.x,
                (_currentCenterGridIndex.y + 0.5f) * _mapData.tileSize.y
            );

            // 실제 캐릭터가 중앙 타일의 정중앙에서 얼만큼 벗어나 있는지 계산
            float offsetX = worldPos.x - centerTileWorldCenter.x;
            float offsetZ = worldPos.z - centerTileWorldCenter.y;

            // 3D 스케일을 2D UI 스케일로 변환하는 비율
            float scaleX = _uiTileSize.x / _mapData.tileSize.x;
            float scaleY = _uiTileSize.y / _mapData.tileSize.y;

            // 플레이어가 이동한 만큼 UI 판데기 자체를 반대 방향으로 밀어줌 (고정형 미니맵)
            _rectTransform.anchoredPosition = new Vector2(-(offsetX * scaleX), -(offsetZ * scaleY));
        }
    }
}