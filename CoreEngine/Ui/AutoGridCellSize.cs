using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.UI
{
    /// <summary>
    /// GridLayoutGroup의 CellSize 동기화 모드
    /// </summary>
    public enum GridCellSizeMode
    {
        /// <summary>
        /// 1. CellSize를 현재 RectTransform의 (Width, Height)와 1:1로 동일하게 맞춤
        /// </summary>
        MatchRectTransform,

        /// <summary>
        /// 2. GridLayoutGroup의 Constraint 설정을 기준으로 가용 공간을 분할하여 꽉 채움
        /// </summary>
        FitGridByConstraint
    }

    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup), typeof(RectTransform))]
    public class AutoGridCellSize : MonoBehaviour
    {
        [Header("Size Mode")]
        [Tooltip("CellSize 계산 방식을 선택합니다.")]
        [SerializeField] private GridCellSizeMode _sizeMode = GridCellSizeMode.MatchRectTransform;

        [SerializeField] private GridLayoutGroup _gridLayout;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Vector2 _lastSize;
        [SerializeField] private GridCellSizeMode _lastMode;

        public Vector2 LastSize=> _lastSize;

        private void Awake()
        {
            _gridLayout = GetComponent<GridLayoutGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }


        private void Start()
        {
            UpdateCellSize();
        }

        // RectTransform의 크기가 변할 때 실행되는 유니티 엔진 콜백
        private void OnRectTransformDimensionsChange()
        {
            UpdateCellSize();
        }

        // 인스펙터에서 변수(enum 등)를 수정했을 때 즉시 반영
        private void OnValidate()
        {
            UpdateCellSize();
        }

        private void UpdateCellSize()
        {
            if (_gridLayout == null || _rectTransform == null) return;

            Vector2 currentSize = _rectTransform.rect.size;

            // 크기 및 모드 변화가 없는 프레임은 연산 스킵 (최적화)
            if (currentSize == _lastSize && _sizeMode == _lastMode && Application.isPlaying) return;
            _lastSize = currentSize;
            _lastMode = _sizeMode;

            Vector2 calculatedCellSize = Vector2.zero;

            switch (_sizeMode)
            {
                case GridCellSizeMode.MatchRectTransform:
                    // 🌟 [모드 1] 타일 하나의 크기 = MiniMapController의 전체 Width, Height
                    calculatedCellSize = currentSize;
                    break;

                case GridCellSizeMode.FitGridByConstraint:
                    // 🌟 [모드 2] GridLayoutGroup의 Constraint를 기준으로 공간 분할

                    // 제약이 Flexible이면 나눌 기준이 없으므로 계산 패스
                    if (_gridLayout.constraint == GridLayoutGroup.Constraint.Flexible)
                    {
                        Debug.LogWarning($"[{gameObject.name}] AutoGridCellSize: FitGrid 모드는 Flexible 제약을 지원하지 않습니다. GridLayoutGroup의 Constraint를 Fixed Column 또는 Fixed Row로 변경해주세요.");
                        return;
                    }

                    int columnCount = 1;
                    int rowCount = 1;

                    // 하드코딩된 변수 대신 GridLayoutGroup의 실제 값을 참조
                    int constraintCount = Mathf.Max(1, _gridLayout.constraintCount);

                    // 자식 객체의 개수를 기반으로 행/열 계산
                    int childCount = 0;
                    foreach (Transform child in transform)
                    {
                        if (child.gameObject.activeSelf) childCount++;
                    }
                    if (childCount == 0) return; // 자식이 없으면 계산 불필요

                    if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                    {
                        columnCount = constraintCount;
                        rowCount = Mathf.CeilToInt((float)childCount / columnCount);
                    }
                    else if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                    {
                        rowCount = constraintCount;
                        columnCount = Mathf.CeilToInt((float)childCount / rowCount);
                    }

                    float availableWidth = currentSize.x - _gridLayout.padding.left - _gridLayout.padding.right - (_gridLayout.spacing.x * (columnCount - 1));
                    float availableHeight = currentSize.y - _gridLayout.padding.top - _gridLayout.padding.bottom - (_gridLayout.spacing.y * (rowCount - 1));

                    float cellWidth = availableWidth / columnCount;
                    float cellHeight = availableHeight / rowCount;

                    calculatedCellSize = new Vector2(cellWidth, cellHeight);
                    break;
            }

            // 최소 0.1 이상의 크기 보장 (에러/화면 깨짐 차단)
            calculatedCellSize.x = Mathf.Max(0.1f, calculatedCellSize.x);
            calculatedCellSize.y = Mathf.Max(0.1f, calculatedCellSize.y);

            // GridLayoutGroup에 최종 적용
            _gridLayout.cellSize = calculatedCellSize;
        }
    }
}