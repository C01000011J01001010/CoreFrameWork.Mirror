using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.Loading
{
    [DefaultExecutionOrder((int)ExecutionOrder.Loading)]
    [RequireComponent(typeof(CanvasGroup))]
    public class SystemLoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image loadingImage;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("Settings")]
        [SerializeField] private float rotationSpeed = -360f; // 초당 회전 속도

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            // 1계층 독립 Update: Time.timeScale = 0 환경에서도 부드럽게 회전
            if (canvasGroup.alpha > 0f)
            {
                loadingImage.transform.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);
            }
        }

        // 외부(LoadingDirector)에서 UI 수치만 갱신하도록 제공하는 API
        public void UpdateVisuals(float progress, string context)
        {
            if (loadingSlider != null) loadingSlider.value = progress;
            if (loadingText != null) loadingText.text = $"{context}\n{(progress * 100f):F2}%";
        }

        // 외부(LoadingDirector)에서 페이드 연출을 할 수 있도록 제공하는 API
        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
    }
}
