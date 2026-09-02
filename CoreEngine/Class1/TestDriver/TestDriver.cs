using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoreEngine.SceneManagement;
using CoreEngine.DesignPattern.Singleton;
using CoreEngine.Settings;

namespace CoreEngine.Test
{
    /// <summary>
    /// 메인 코드를 1도 건드리지 않는 단독 씬 부트스트래퍼.
    /// [DefaultExecutionOrder]를 통해 씬 내의 어떤 객체보다 가장 먼저 깨어나서 타임라인을 통제합니다.
    /// </summary>
    [DefaultExecutionOrder((int)ExecutionOrder.TestDriver)] // 유니티 엔진 내에서 무조건 최우선으로 Awake() 실행 보장
    public class TestDriver : Singleton<TestDriver> // 1개만 존재해야하니 싱글톤으로 만들되, 절대 싱글톤 접근 사용 안함
    {
        public static SceneReference GlobalScene => CoreEngineSettingsSO.Instance.GlobalScene;

        public static Scene TestScene { get; private set; } = default;

        public static bool IsSceneTest => TestScene.IsValid() && TestScene.isLoaded;

        private readonly List<GameObject> targetRoots = new List<GameObject>();


        // [중요] 에디터에서 플레이 버튼을 누를 때마다 static 변수 초기화 (메모리 누수 및 오작동 방지)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDomain()
        {
            TestScene = default;
        }

        protected override void Awake()
        {
            base.Awake();

            if (SceneManager.sceneCount == 1)
            {
                TestScene = gameObject.scene;

                if (TestScene.buildIndex == -1)
                {
                    Debug.LogWarning($"<color=yellow>[SceneTester]</color> '{TestScene.name}' 씬은 Build Settings에 미등록된 씬입니다.");
                }

                // 🔥 [가장 중요한 변화] 내가 제일 먼저 깨어났으므로, 다른 애들이 Awake()를 돌리기 전에 전부 기절시킴!
                GameObject[] rootObjects = TestScene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    // 1. (부모객체인 SceneContext를 포함하여) SceneTester 자신 제외
                    if (root == transform.root.gameObject) continue;

                    if (root.activeSelf)
                    {
                        targetRoots.Add(root);
                        root.SetActive(false); // 이 순간 나머지 객체들은 Awake조차 실행되지 못하고 동면 상태 돌입
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (IsSceneTest)
            {
                StartCoroutine(TestBootstrapSequence());
            }
        }

        private IEnumerator TestBootstrapSequence()
        {
            // 1. [코어 로드] 전역 씬 Additive 로드
            SceneManager.LoadSceneAsync(GlobalScene, LoadSceneMode.Additive);

            // SceneContext가 초기화 될때까지 대기
            if (SceneContext.Inst != null)
            {
                while(!SceneContext.IsInit)
                {
                    yield return null;
                }
            }

            // 3. [재부팅] 전역 코어 세팅이 완료되었으므로, 동면했던 객체들을 깨움
            Debug.Log($"[SceneTester] 전역 코어 세팅 완료! 동면 중이던 {targetRoots.Count}개의 루트 객체를 깨웁니다.");

            foreach (var root in targetRoots)
            {
                if (root != null)
                {
                    // 이제서야 이 객체들의 Awake -> OnEnable -> Start 가 정상적인 라이프사이클대로 흐르기 시작함!
                    root.SetActive(true);
                }
            }

            // 4. 임무 완료 후 자폭
            Debug.Log("[SceneTester] 단독 씬 테스트 환경 구축 성공. 부트스트래퍼를 종료합니다.");
            Destroy(gameObject);
        }

    }
}

