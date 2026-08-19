using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.SceneManagement
{
    /// <summary>
    /// 에디터에서는 SceneAsset 드래그 앤 드롭을 지원하고,
    /// 런타임에서는 string 씬 이름/경로로 안전하게 변환되는 씬 참조 구조체
    /// </summary>
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

        // 런타임용 씬 경로 및 이름
        [SerializeField] private string scenePath = string.Empty;
        [SerializeField] private string sceneName = string.Empty;

        public string SceneName => sceneName;
        public string ScenePath => scenePath;

        // 에디터에서 값이 변경되거나 저장될 때 자동 호출
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                sceneName = sceneAsset.name;
            }
            else
            {
                scenePath = string.Empty;
                sceneName = string.Empty;
            }
#endif
        }

        public void OnAfterDeserialize() { }

        // string으로 자동 형변환 지원 (편의성)
        public static implicit operator string(SceneReference sceneRef)
        {
            return sceneRef?.SceneName ?? string.Empty;
        }
    }
}