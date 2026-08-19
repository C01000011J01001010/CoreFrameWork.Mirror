using CoreEngine.Director;
using CoreEngine.EventBus;
using System.Collections;
using UnityEngine;

namespace CoreEngine.Loading
{
    // Ui를 직접 다루기에 Director로써 특수한 초기화순서
    [DefaultExecutionOrder((int)ExecutionOrder.Loading)]
    internal class LoadingDirector : BaseDirector<LoadingDirector>
    {
        [Header("UI Reference")]
        [SerializeField] private SystemLoadingScreen loadingScreen;

        [Header("Settings")]
        [SerializeField] private float fadeOutDuration = 0.5f; // 로딩 종료 시 페이드 아웃 시간

        private Coroutine fadeCoroutine;

        private void OnEnable()
        {
            EventBus<SystemLoadingEvent>.Subscribe(OnLoadingEventReceived);
        }

        private void OnDisable()
        {
            EventBus<SystemLoadingEvent>.Unsubscribe(OnLoadingEventReceived);
        }

        // 3. SystemLoadingEvent의 올바른 프로퍼티(State, Message, Progress)를 사용하도록 수정
        private void OnLoadingEventReceived(SystemLoadingEvent evt)
        {
            switch (evt.LoadingState)
            {
                case SystemLoadingEvent.State.Start:
                case SystemLoadingEvent.State.Progress:
                    // 시작하거나 진행 중일 때는 화면을 띄우고 상태를 업데이트합니다.
                    ShowLoading(evt.Progress, evt.Message);
                    break;

                case SystemLoadingEvent.State.Complete:
                    // 완료되었을 때 페이드아웃을 시작합니다.
                    HideLoading();
                    break;
            }
        }

        private void ShowLoading(float progress, string message)
        {
            // 만약 페이드 아웃 연출 중에 다시 로딩이 불렸다면 중단하고 즉시 활성화
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            loadingScreen.gameObject.SetActive(true);
            loadingScreen.SetAlpha(1f);

            // SystemLoadingScreen 클래스 내부 메서드도 파라미터 이름을 message로 받도록 맞춰주세요.
            loadingScreen.UpdateVisuals(progress, message);
        }

        private void HideLoading()
        {
            // 씬이 비활성화 상태가 아닐 때만 코루틴 실행
            if (gameObject.activeInHierarchy)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeOutCoroutine());
            }
            else
            {
                loadingScreen.SetAlpha(0f);
                loadingScreen.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeOutCoroutine()
        {
            float timer = 0f;

            // unscaledDeltaTime을 사용하여 일시정지(타임스케일 0) 씬에서도 부드럽게 페이드 아웃
            while (timer < fadeOutDuration)
            {
                timer += Time.unscaledDeltaTime;
                float currentAlpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
                loadingScreen.SetAlpha(currentAlpha);
                yield return null;
            }

            loadingScreen.SetAlpha(0f);
            loadingScreen.gameObject.SetActive(false);
            fadeCoroutine = null;
        }
    }
}
