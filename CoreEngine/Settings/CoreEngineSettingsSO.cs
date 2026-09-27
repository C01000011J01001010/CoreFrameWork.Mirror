using CoreEngine.SceneManagement;
using UnityEngine;

namespace CoreEngine.Settings
{

    public class CoreEngineSettingsSO : BaseGameSettingsSO<CoreEngineSettingsSO>
    {
        // 데이터와 동기화 로직을 모두 캡슐화한 Tracker 선언
        [SerializeField] 
        private AutoBuildSceneTracker _sceneTracker = new AutoBuildSceneTracker();

        // 런타임에서 다른 매니저들이 안전하게 읽을 수 있도록 프로퍼티 개방
        public SceneReference GlobalScene => _sceneTracker.globalScene;
        public SceneReference[] ExtensionSceneList => _sceneTracker.extensionSceneList;
        public SceneReference FirstScene => _sceneTracker.firstScene;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sceneTracker.Validate(this);
        }
#endif
    }
}