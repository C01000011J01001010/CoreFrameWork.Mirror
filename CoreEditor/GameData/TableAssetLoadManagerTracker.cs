#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using CoreEngine.Settings;
using CoreEngine.GameData; // TableAssetLoadManager 위치

namespace CoreEditor.PostProcessors
{
    public class TableAssetLoadManagerTracker : AssetModificationProcessor
    {
        // 에디터에서 씬을 저장하기 직전에 호출됨
        public static string[] OnWillSaveAssets(string[] paths)
        {
            var autoSettings = CoreEngineAutoSettingsSO.Instance;
            var settings = CoreEngineSettingsSO.Instance;

            if (autoSettings == null || settings == null) return paths;

            bool isDirty = false;

            foreach (var path in paths)
            {
                if (path.EndsWith(".unity")) // 씬 파일 저장 감지
                {
                    var scene = SceneManager.GetSceneByPath(path);
                    if (scene.isLoaded)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);

                        // 현재 저장중인 씬 내부에 TableAssetLoadManager가 있는지 검색
                        bool hasManager = false;
                        var rootObjects = scene.GetRootGameObjects();
                        foreach (var root in rootObjects)
                        {
                            if (root.GetComponentInChildren<TableAssetLoadManager>(true) != null)
                            {
                                hasManager = true;
                                break;
                            }
                        }

                        // AutoSettings 및 SettingsSO 동기화
                        if (hasManager && !autoSettings.ManagedSceneGUIDs.Contains(guid))
                        {
                            autoSettings.SyncManagedScene(guid, true);
                            isDirty = true;
                        }
                        else if (!hasManager && autoSettings.ManagedSceneGUIDs.Contains(guid))
                        {
                            autoSettings.SyncManagedScene(guid, false);
                            settings.RemoveDependency(guid); // 매핑 리스트에서도 즉시 삭제
                            isDirty = true;
                        }
                    }
                }
            }

            if (isDirty)
            {
                AssetDatabase.SaveAssets();
            }

            return paths;
        }
    }
}
#endif