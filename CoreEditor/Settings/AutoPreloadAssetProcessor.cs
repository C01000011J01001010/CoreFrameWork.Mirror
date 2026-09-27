using CoreEngine.Settings;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.Settings
{
    [InitializeOnLoad] // 에디터가 로드되거나 스크립트가 컴파일될 때 자동 실행
    public static class AutoPreloadAssetProcessor
    {
        static AutoPreloadAssetProcessor()
        {
            RefreshPreloadedAssets();
        }

        public static void RefreshPreloadedAssets()
        {
            // [AutoPreloadAsset] 어트리뷰트가 달린 모든 타입(클래스)을 찾음
            var allScriptableObjects = TypeCache.GetTypesDerivedFrom<ScriptableObject>();

            var types = allScriptableObjects // 모든 ScriptableObject 중에서
                .Where(t => !t.IsAbstract && !t.IsGenericType) // 제네릭도 추상클래스도 아닌 상세클래스를 찾고
                .Where(t => t.GetCustomAttributes(typeof(AutoPreloadAssetAttribute), true).Length > 0) // 그 중 Attribute를 갖는 것만 추려냄
                .ToList();

            if (types.Count == 0) return;

            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            bool isModified = false;

            // 프로젝트에 찌꺼기(Null)가 있다면 청소
            if (preloadedAssets.RemoveAll(a => a == null) > 0)
            {
                isModified = true;
            }

            // 해당 타입으로 생성된 SO 에셋을 찾아 리스트에 없으면 자동 추가
            foreach (var type in types)
            {
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    if (asset != null && !preloadedAssets.Contains(asset))
                    {
                        preloadedAssets.Add(asset);
                        isModified = true;
                        Debug.Log($"[Core Framework] '{asset.name}' 객체가 Preloaded Assets에 자동 등록되었습니다.", asset);
                    }
                }
            }

            // 변경 사항이 있으면 PlayerSettings 덮어쓰기 및 저장
            if (isModified)
            {
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
                AssetDatabase.SaveAssets();
            }
        }
    }
}
