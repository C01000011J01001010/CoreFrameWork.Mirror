using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CoreEngine.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIDraggable : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Serializable]
        private struct AlphaSetting
        {
            [Tooltip("드래그 중 투명도/레이캐스트 제어를 위한 대상 (선택 사항)")]
            public CanvasGroup targetCanvasGroup;

            [Tooltip("드래그 중 투명도 배율 (초기 alpha 대비 상대값)"), Range(0f, 1f)]
            public float dragAlphaMultiplier;
        }

        [Header("Drag Target Settings")]
        [Tooltip("실제 움직일 대상 (비워두면 부모 창 또는 자기 자신의 BaseUi 객체 자동 추적)")]
        [SerializeField] private RectTransform _moveTarget;
        [SerializeField] private AlphaSetting _alphaSetting = new AlphaSetting { dragAlphaMultiplier = 0.8f };

        private Canvas _canvas;
        private float _originalAlpha;

        // 멀티터치 방어용 ID 캐싱
        private int _currentPointerId = -1000;

        private void Awake()
        {
            // 1. 타겟 미지정 시: 부모 중 전체 창(BaseUi)이 있다면 그것을, 없다면 자기 자신을 타겟으로 설정
            if (_moveTarget == null)
            {
                // BaseUi가 있다면 가져오고, 없으면 null 반환
                var parentWindow = GetComponentInParent<BaseUi>(); // 필요시 BaseUi로 교체
                _moveTarget = parentWindow != null ? parentWindow.GetComponent<RectTransform>() : GetComponent<RectTransform>();
            }

            if (_alphaSetting.targetCanvasGroup == null)
                _alphaSetting.targetCanvasGroup = GetComponent<CanvasGroup>();

            _canvas = GetComponentInParent<Canvas>();

            if (_canvas == null)
            {
                Debug.LogError($"[{gameObject.name}] UIDraggable은 Canvas의 자식이어야 합니다!");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _moveTarget.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 이미 다른 손가락으로 드래그 중이면 새로운 터치 무시
            if (_currentPointerId != -1000) return;
            _currentPointerId = eventData.pointerId;

            if (_alphaSetting.targetCanvasGroup != null)
            {
                _originalAlpha = _alphaSetting.targetCanvasGroup.alpha;
                _alphaSetting.targetCanvasGroup.alpha = _alphaSetting.dragAlphaMultiplier * _originalAlpha;
                _alphaSetting.targetCanvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 최초 터치한 손가락의 ID가 아니거나 캔버스가 없으면 무시
            if (eventData.pointerId != _currentPointerId || _canvas == null) return;

            _moveTarget.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _currentPointerId) return;
            _currentPointerId = -1000; // 터치 ID 초기화

            if (_alphaSetting.targetCanvasGroup != null)
            {
                _alphaSetting.targetCanvasGroup.alpha = _originalAlpha;
                _alphaSetting.targetCanvasGroup.blocksRaycasts = true;
            }
        }

        private void Reset()
        {
            _alphaSetting.dragAlphaMultiplier = 0.7f;
        }
    }
}