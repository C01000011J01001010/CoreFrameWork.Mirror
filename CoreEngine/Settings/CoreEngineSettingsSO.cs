using CoreEngine.SceneManagement;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreEngine.Settings
{
    // 프로젝트마다 이 SO 에셋을 딱 1개만 만들어서 사용
    [CreateAssetMenu(fileName = "CoreEngineSettings", menuName = "CoreEngine/Settings")]
    public class CoreEngineSettingsSO : ScriptableObject
    {
        // 데이터와 동기화 로직을 모두 캡슐화한 Tracker 선언
        [SerializeField] 
        private AutoBuildSceneTracker _sceneTracker = new AutoBuildSceneTracker();

        // 런타임에서 다른 매니저들이 안전하게 읽을 수 있도록 프로퍼티 개방
        public SceneReference GlobalScene => _sceneTracker.globalScene;
        public SceneReference[] ExtensionSceneList => _sceneTracker.extensionSceneList;

        private static CoreEngineSettingsSO _instance;
        public static CoreEngineSettingsSO Instance
        {
            get
            {
                if (_instance != null) return _instance;

#if UNITY_EDITOR
                // 1. 에셋 데이터베이스에서 기존 설정 에셋 검색
                string[] guids = AssetDatabase.FindAssets("t:CoreEngineSettingsSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = AssetDatabase.LoadAssetAtPath<CoreEngineSettingsSO>(path);

                    // 만약 강제로 2개 이상 복제되었다면 경고 후 첫 번째 것만 사용
                    if (guids.Length > 1)
                    {
                        Debug.LogWarning("[CoreEngine] CoreEngineSettingsSO 에셋이 2개 이상 발견되었습니다. 구조적 오류를 막기 위해 첫 번째 에셋만 사용됩니다.");
                    }
                }
                else
                {
                    // 하나도 없다면 정해진 경로에 자동 생성
                    _instance = CreateInstance<CoreEngineSettingsSO>();
                    string folderPath = "Assets/Settings/CoreFramework";

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string assetPath = $"{folderPath}/CoreEngineSettings.asset";
                    AssetDatabase.CreateAsset(_instance, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    Debug.Log($"[CoreEngine] 필수 설정 에셋이 없어서 자동으로 생성되었습니다: {assetPath}");
                }
#endif
                return _instance;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sceneTracker.Validate(this);
        }
#endif
    }
}