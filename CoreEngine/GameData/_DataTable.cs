using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    internal interface IPreloadAssetIdsBaker
    {
#if UNITY_EDITOR
        void BakePreloadAssetIds(_AssetId[] bakedIds);
#endif
    }
    public abstract class _DataTable : ScriptableObject, IPreloadAssetIdsBaker
    {
        // [핵심] SerializeReference를 통해 자식 제네릭 클래스들의 다형성을 유지하며 저장
        [SerializeReference, ReadOnly]
        private _AssetId[] _preloadAssetIds;

        // ==========================================
        // [런타임 전용] TableAssetLoadManager가 호출할 API
        // ==========================================
        internal async Task PreloadAssetsAsync()
        {
            if (_preloadAssetIds == null || _preloadAssetIds.Length == 0) return;

            var loadTasks = new List<Task>(_preloadAssetIds.Length);
            foreach (var assetId in _preloadAssetIds)
            {
                if (assetId != null && assetId.Id > 0)
                {
                    loadTasks.Add(assetId.LoadAsync());
                }
            }

            // 배열 내 모든 에셋의 로드가 끝날 때까지 병렬 대기
            await Task.WhenAll(loadTasks);
        }

        internal void ReleaseAssets()
        {
            if (_preloadAssetIds == null || _preloadAssetIds.Length == 0) return;

            foreach (var assetId in _preloadAssetIds)
            {
                if (assetId != null && assetId.Id > 0)
                {
                    assetId.Release();
                }
            }
        }

#if UNITY_EDITOR
        void IPreloadAssetIdsBaker.BakePreloadAssetIds(_AssetId[] bakedIds)
        {
            _preloadAssetIds = bakedIds;
        }
#endif
    }
}