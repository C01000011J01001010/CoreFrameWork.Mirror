using UnityEngine;
using System.Threading.Tasks;

namespace CoreEngine.GameData
{
    public static class AssetRouter<TAsset> where TAsset : Object
    {
        private static BaseAssetRegistry<TAsset> _registry;

        // GameDataManager가 어드레서블로 로드한 뒤 이 메서드를 호출해 주입
        internal static void InjectRegistry(BaseAssetRegistry<TAsset> registry)
        {
            _registry = registry;
            // 중복이라 제거
            //_registry.InitializeRuntimeCache(); // 주입받을 때 딕셔너리 초기화!
        }

        public static TAsset Get(int id)
        {
            if (_registry == null) return null;
            return _registry.GetAsset(id);
        }

        public static Task<TAsset> LoadAsync(int id)
        {
            if (_registry == null) return Task.FromResult<TAsset>(null);
            return _registry.LoadAssetAsync(id);
        }

        public static void Release(int id)
        {
            if (_registry != null)
                _registry.ReleaseAsset(id);
        }
    }
}