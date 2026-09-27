using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    public abstract class _DataTable : ScriptableObject
    {
        // [핵심] SerializeReference를 통해 자식 제네릭 클래스들의 다형성을 유지하며 저장
        [SerializeReference]
        private _AssetId[] _preloadAssetIds;

        // ==========================================
        // [런타임 전용] TableAssetLoadManager가 호출할 API
        // ==========================================
        public async Task PreloadAssetsAsync()
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

        public void ReleaseAssets()
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

        // ==========================================
        // [에디터 전용] CsvToTableBatchProcessor가 컨버팅 시 호출
        // ==========================================
#if UNITY_EDITOR
        public void Editor_BakePreloadAssetIds(_AssetId[] bakedIds)
        {
            _preloadAssetIds = bakedIds;
        }
#endif
    }
}