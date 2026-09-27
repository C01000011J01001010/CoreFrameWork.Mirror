using CoreEngine.Facades;
using CoreEngine.Resource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    internal class BaseAssetRegistry<TAsset> : _AssetRegistry where TAsset : UnityEngine.Object
    {
        [Serializable]
        public struct AssetEntry
        {
            public int Id;
            public string PathDetail;
        }

        [SerializeField] private string _baseDirectory;
        [SerializeField] private List<AssetEntry> _entries = new List<AssetEntry>();

        private Dictionary<int, string> _runtimeAddressDict;

        // [최적화] 매번 탐색하지 않도록 ResourceManager를 캐싱합니다.
        private ResourceManager _resourceManager;

        public override void InitializeRuntimeCache()
        {
            // 1. 매니저 캐싱 (단 1회 수행)
            _resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (_resourceManager == null)
            {
                Debug.LogError("[BaseAssetRegistry] ResourceManager를 찾을 수 없습니다!");
            }

            // 2. 주소 딕셔너리 구성
            _runtimeAddressDict = new Dictionary<int, string>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (entry.Id > 0)
                {
                    _runtimeAddressDict.Add(entry.Id, $"{_baseDirectory}/{entry.PathDetail}");
                }
            }
        }

        public TAsset GetAsset(int id)
        {
            if (_runtimeAddressDict == null || !_runtimeAddressDict.TryGetValue(id, out string address))
                return null;

            // [최적화] 캐싱된 매니저를 즉시 사용 (탐색 비용 제거)
            return _resourceManager?.LoadSceneAssetSync<TAsset>(address);
        }

        public Task<TAsset> LoadAssetAsync(int id)
        {
            if (_runtimeAddressDict == null || !_runtimeAddressDict.TryGetValue(id, out string address))
                return Task.FromResult<TAsset>(null);

            return _resourceManager != null ? _resourceManager.LoadSceneAssetAsync<TAsset>(address) : Task.FromResult<TAsset>(null);
        }

        public void ReleaseAsset(int id)
        {
            if (_runtimeAddressDict == null || !_runtimeAddressDict.TryGetValue(id, out string address))
                return;

            // 캐싱된 ResourceManager를 통해 어드레서블 릴리즈 요청
            _resourceManager?.ReleaseSceneAsset(address);
        }
    }
}