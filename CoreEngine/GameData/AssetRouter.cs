using UnityEngine;
using System.Threading.Tasks;
using CoreEngine.GameData;

namespace CoreEngine.GameData
{
    /// <summary>
    /// AssetId와 AssetRegistry 사이를 연결해주는 정적 라우터입니다.
    /// 제네릭 타입별로 독립된 메모리 공간을 가져 가장 빠른 조회 속도(O(1))를 보장합니다.
    /// </summary>
    public static class AssetRouter<TAsset> where TAsset : Object
    {
        private static BaseAssetRegistry<TAsset> _registry;

        // 레지스트리를 지연 로딩(Lazy Loading)으로 가져오는 핵심 프로퍼티
        private static BaseAssetRegistry<TAsset> Registry
        {
            get
            {
                if (_registry != null)
                    return _registry;

                // 1. 메모리에 없다면 Resources 폴더에서 에셋 타입 이름으로 레지스트리를 찾습니다.
                // 규칙: Resources/Registries/SpriteRegistry.asset 형태로 파일이 존재해야 합니다.
                string path = $"Registries/{typeof(TAsset).Name}Registry";
                _registry = Resources.Load<BaseAssetRegistry<TAsset>>(path);

                if (_registry != null)
                {
                    // 2. 레지스트리를 찾았다면 런타임용 딕셔너리를 한 번 초기화해줍니다.
                    _registry.InitializeRuntimeCache();
                }
                else
                {
                    Debug.LogError($"[AssetRouter] {path} 경로에서 레지스트리를 찾을 수 없습니다. Resources 폴더와 파일명을 확인하세요.");
                }

                return _registry;
            }
        }

        // 동기 즉시 로드 (또는 강제 로드)
        public static TAsset Get(int id)
        {
            if (Registry == null) return null;
            return Registry.GetAsset(id);
        }

        // 비동기 사전 로드
        public static Task<TAsset> LoadAsync(int id)
        {
            if (Registry == null) return Task.FromResult<TAsset>(null);
            return Registry.LoadAssetAsync(id);
        }
    }
}