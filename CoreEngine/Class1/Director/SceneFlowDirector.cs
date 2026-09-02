using CoreEngine.EventBus;
using CoreEngine.Test;
using CoreEngine.Helpers;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoreEngine.Loading;

namespace CoreEngine
{
    public struct SceneLoadRequestEvent : IEvent
    {
        public string TargetSceneName;
        public SceneLoadRequestEvent(string targetSceneName)
        {
            TargetSceneName = targetSceneName;
        }
    }
}

namespace CoreEngine.Test
{
    /// <summary>
    /// 단독 씬 플레이 테스트 시, 이미 로드된 씬의 시스템 초기화를 요청하는 이벤트
    /// </summary>
    public struct SceneTestBootstrapRequestEvent : IEvent
    {
        public Scene TestScene;
        public SceneTestBootstrapRequestEvent(Scene scene) => TestScene = scene;
    }
}

namespace CoreEngine.Director
{
    [DefaultExecutionOrder((int)ExecutionOrder.SceneFlowDirector)]
    public class SceneFlowDirector : BaseDirector<SceneFlowDirector>
    {
        protected static Scene currentScene;
        protected static AsyncOperation sceneChangeProgress;

        /// <summary>
        /// SceneLoadRequestEvent 또는 SceneTestBootstrapRequestEvent 둘 중 하나를 수행중인지 판단하는 플래그
        /// </summary>
        protected bool _isRoutine;

        protected virtual void OnEnable()
        {
            EventBus<SceneLoadRequestEvent>.Subscribe(OnSceneLoadRequest);
            EventBus<SceneTestBootstrapRequestEvent>.Subscribe(OnTestBootstrapRequest);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        protected virtual void OnDisable()
        {
            EventBus<SceneLoadRequestEvent>.Unsubscribe(OnSceneLoadRequest);
            EventBus<SceneTestBootstrapRequestEvent>.Unsubscribe(OnTestBootstrapRequest);

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        // EventBus를 통해 씬 전환 요청이 들어왔을 때 실행되는 핸들러
        protected virtual void OnSceneLoadRequest(SceneLoadRequestEvent evt)
        {
            LogHelper.LogFunctionCallStart(this);
            if(!_isRoutine)
            {
                _isRoutine = true;
                _ = ChangeScene(evt.TargetSceneName);
            }
        }

        protected async Task ChangeScene(string sceneName)
        {
            // 수정된 SystemLoadingEvent 구조체(State, Message, Progress) 규격에 맞게 발송
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Start, "이전 데이터 정리 중...", 0.0f));

            if (currentScene.IsValid() && currentScene.isLoaded)
            {
                // 현재 씬을 비활성화하고 언로드
                SceneContext.Inst.gameObject.SetActive(false);
                await SceneManager.UnloadSceneAsync(currentScene);
            }

            StartCoroutine(LoadingScene(sceneName));
        }

        protected IEnumerator LoadingScene(string sceneName)
        {
            sceneChangeProgress = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            sceneChangeProgress.allowSceneActivation = false;

            while (sceneChangeProgress.progress < 0.9f)
            {
                float loadProgress = Mathf.Clamp01(sceneChangeProgress.progress / 0.9f);

                // Progress 중계
                EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, $"{sceneName} 자원 불러오는 중...", loadProgress * 0.7f));
                yield return null;
            }

            sceneChangeProgress.allowSceneActivation = true;

            while (!sceneChangeProgress.isDone) yield return null;

            yield return InitializeSceneSystemRoutine();
        }

        // 단독 씬 테스트 시작 요청을 받았을 때
        protected void OnTestBootstrapRequest(SceneTestBootstrapRequestEvent evt)
        {
            LogHelper.LogFunctionCallStart(this);
            if (!_isRoutine)
            {
                _isRoutine = true;
                StartCoroutine(BootstrapTestSceneRoutine(evt.TestScene));
            }
        }

        /// <summary>
        /// 단독 씬 테스트용 부트스트랩 루틴 (자원 로드 생략)
        /// </summary>
        protected IEnumerator BootstrapTestSceneRoutine(Scene testScene)
        {
            // [상단 부 생략] 이미 씬이 있으므로 캐싱만 수행
            currentScene = testScene;

            // DX(개발자 경험)를 위해 테스트 시작 연출용 가짜(?) 진행도 살짝 표기
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "단독 씬 테스트 환경 감지...", 0.3f));
            yield return new WaitForSecondsRealtime(0.1f); // 엔진 안정화용 미세 대기

            // [하단 부 공유] 시스템 환경 구축 시퀀스로 진입
            yield return InitializeSceneSystemRoutine();
        }

        /// <summary>
        /// [공통 파이프라인] 씬 내부의 시스템 구축 및 초기화 단계
        /// </summary>
        protected IEnumerator InitializeSceneSystemRoutine()
        {
            // 안전하게 현재씬을 다시 고정
            SceneManagementHelper.SetActiveScene(currentScene);

            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "새로운 시스템 환경 세팅 중...", 0.85f));

            // 핵심: 디렉터가 전권을 가지고 씬 컨텍스트 산하 Hub들을 깨움
            if (SceneContext.Inst != null)
            {
                yield return SceneContext.Inst.Initialize();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 현재 씬에 SceneContext가 존재하지 않습니다.");
            }

            yield return LoadingDirector.DelayLoadingForTooltip();

            // 로딩 최종 완료 공포
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Complete, "로딩 완료!", 1.0f));

            _isRoutine = false;
        }


        /// <summary>
        /// 에디터 테스트 및 런타임 초기화 시, SceneContext가 자신의 씬 정보를 다이렉트로 등록하기 위한 함수
        /// </summary>
        public static void RegisterCurrentScene(Scene newScene)
        {
            currentScene = newScene;
            SceneManagementHelper.SetActiveScene(currentScene);
        }

        // 필요시 재정의
        protected virtual void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedMode) { }
        protected virtual void OnSceneUnloaded(Scene unloadedScene) { }
        protected virtual void OnActiveSceneChanged(Scene prevScene, Scene newScene) { }
    }
}
