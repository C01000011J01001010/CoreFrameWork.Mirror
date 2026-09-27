using CoreEngine.GameData;
using CoreEngine.SceneManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Settings
{
    [Serializable]
    public class SceneTableDependency
    {
        [HideInInspector] public string targetSceneGUID; // 매핑 고유키
        public SceneReference targetScene;
        public List<TableReference> requiredTables = new List<TableReference>();
    }

    public class CoreEngineSettingsSO : BaseGameSettingsSO<CoreEngineSettingsSO>
    {
        // 데이터와 동기화 로직을 모두 캡슐화한 Tracker 선언
        [SerializeField] 
        private AutoBuildSceneTracker _sceneTracker = new AutoBuildSceneTracker();
        [SerializeField]
        private List<SceneTableDependency> _sceneTableDependencies = new List<SceneTableDependency>();
        

        // 런타임에서 다른 매니저들이 안전하게 읽을 수 있도록 프로퍼티 개방
        public SceneReference GlobalScene => _sceneTracker.globalScene;
        public SceneReference[] ExtensionSceneList => _sceneTracker.extensionSceneList;
        public SceneReference FirstScene => _sceneTracker.firstScene;
        public List<SceneTableDependency> SceneTableDependencies => _sceneTableDependencies;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sceneTracker.Validate(this);
        }

        public void AddDependency(string guid, SceneReference sceneRef)
        {
            if (_sceneTableDependencies.Find(x => x.targetSceneGUID == guid) == null)
            {
                _sceneTableDependencies.Add(new SceneTableDependency { targetSceneGUID = guid, targetScene = sceneRef });
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        public void RemoveDependency(string guid)
        {
            _sceneTableDependencies.RemoveAll(x => x.targetSceneGUID == guid);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}