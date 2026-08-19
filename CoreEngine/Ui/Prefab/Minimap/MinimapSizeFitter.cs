using CoreEngine;
using UnityEngine;

namespace CoreEngine.Ui
{
    /// <summary>
    /// 내부 UI 요소(이름 텍스트, 지도 캔버스)의 세로 높이에 맞춰 
    /// 부모 캔버스(배경)의 세로 크기를 동적으로 조절해주는 커스텀 핏터
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MinimapSizeFitter : CoreMonoBehaviour
    {
        [Header("Target UI Elements")]
        [Tooltip("세로 공간을 차지할 모든 ui 구성품")]
        [SerializeField] private RectTransform[] _targetRectTransforms;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 지도 이미지를 로드하고 높이를 조절한 직후에 이 함수를 호출하면, 
        /// 배경 UI의 세로 크기가 자식들의 크기에 맞춰 자동으로 늘어납니다.
        /// </summary>
        public void AdjustHeight()
        {
            if(_targetRectTransforms == null || _targetRectTransforms.Length < 1)
            {
                UtilityLog.Log("[MinimapSizeFitter] 타겟 UI 요소가 할당되지 않았습니다.", LogColor.Yellow);
                return;
            }

            float totalHeight = 0;
            for(int i = 0; i < _targetRectTransforms.Length; i++ )
            {
                if (_targetRectTransforms[i] != null) totalHeight += _targetRectTransforms[i].rect.height;
            }

            // 3. 부모 캔버스의 가로 크기(x)는 그대로 유지하고, 세로 크기(y)만 덮어쓰기
            Vector2 newSize = _rectTransform.sizeDelta;
            newSize.y = totalHeight;
            _rectTransform.sizeDelta = newSize;
        }
    }
}