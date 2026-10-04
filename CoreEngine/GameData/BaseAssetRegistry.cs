using CoreEngine.Facades;
using CoreEngine.Resource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    /// <summary>
    /// Type 안정성을 위해 제네릭 유지
    /// </summary>
    public class BaseAssetRegistry<TAsset> : 
        _AssetRegistry where TAsset : Object
    {
        [Serializable]
        public struct AssetEntry : IIdentifiable
        {
            public ulong Id;
            public string PathDetail;

            ulong IIdentifiable.ID => Id;
        }

        [SerializeField] private string _baseDirectory;
        [SerializeField] private List<AssetEntry> _entries = new List<AssetEntry>();

        private Dictionary<ulong, string> _runtimeAddressDict;

        // ResourceManager를 캐싱
        private ResourceManager _resourceManager;

        private void OnEnable()
        {
            // 만약의 쓰레기 객체 정리용
            _runtimeAddressDict = null;
        }

        public override void InitializeRuntimeCache()
        {
            // 매니저 캐싱 (단 1회 수행)
            _resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (_resourceManager == null)
            {
                Debug.LogError("[BaseAssetRegistry] ResourceManager를 찾을 수 없습니다!");
            }

            // 주소 딕셔너리 구성
            _runtimeAddressDict = new(_entries.Count);
            foreach (var entry in _entries)
            {
                if (entry.Id > 0)
                {
                    _runtimeAddressDict.Add(entry.Id, $"{_baseDirectory}/{entry.PathDetail}");
                }
            }
        }

        public override Object GetAsset(ulong id)
        {
            if (!TryGetValidAddress(id, out string address))
                return null;

            return _resourceManager?.LoadSceneAssetSync<Object>(address);
        }

        public override Task<Object> LoadAssetAsync(ulong id)
        {
            if (!TryGetValidAddress(id, out string address))
                return Task.FromResult<Object>(null);

            if (_resourceManager != null)
            {
                return _resourceManager.LoadSceneAssetAsync<Object>(address);
            }
            else
            {
                return Task.FromResult<Object>(null);
            }
        }

        public override void ReleaseAsset(ulong id)
        {
            if (!TryGetValidAddress(id, out string address))
                return;

            // 캐싱된 ResourceManager를 통해 어드레서블 릴리즈 요청
            _resourceManager?.ReleaseSceneAsset(address);
        }

        private bool TryGetValidAddress(ulong id, out string address)
        {
            if (_runtimeAddressDict == null) InitializeRuntimeCache();

            return _runtimeAddressDict.TryGetValue(id, out address);
        }
    }
}