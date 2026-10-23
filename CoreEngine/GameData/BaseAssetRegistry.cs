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
    /// Type 안정성 및 서브 에셋(Sprite 등) 정밀 로드를 지원하는 제네릭 레지스트리 베이스
    /// </summary>
    public class BaseAssetRegistry<TAsset> : _AssetRegistry where TAsset : Object
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
        private ResourceManager _resourceManager;

        private void OnEnable()
        {
            _runtimeAddressDict = null;
        }

        public override void InitializeRuntimeCache()
        {
            _resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (_resourceManager == null)
            {
                Debug.LogError("[BaseAssetRegistry] ResourceManager를 찾을 수 없습니다!");
            }

            _runtimeAddressDict = new Dictionary<ulong, string>(_entries.Count);
            string cleanBaseDir = string.IsNullOrEmpty(_baseDirectory) ? string.Empty : _baseDirectory.TrimEnd('/');

            foreach (var entry in _entries)
            {
                if (entry.Id > 0)
                {
                    _runtimeAddressDict[entry.Id] = $"{cleanBaseDir}/{entry.PathDetail}";
                }
            }
        }

        public override Object GetAsset(ulong id) => GetTypedAsset(id);

        /// <summary>
        /// TAsset 타입을 명시하여 서브 에셋(Sprite 등)까지 정확히 동기 로드합니다.
        /// </summary>
        public TAsset GetTypedAsset(ulong id)
        {
            if (!TryGetValidAddress(id, out string address))
                return null;

            return _resourceManager?.LoadSceneAssetSync<TAsset>(address);
        }

        public override async Task<Object> LoadAssetAsync(ulong id) => await LoadTypedAssetAsync(id);

        /// <summary>
        /// TAsset 타입을 명시하여 서브 에셋(Sprite 등)까지 정확히 비동기 로드합니다.
        /// </summary>
        public Task<TAsset> LoadTypedAssetAsync(ulong id)
        {
            if (!TryGetValidAddress(id, out string address) || _resourceManager == null)
                return Task.FromResult<TAsset>(null);

            return _resourceManager.LoadSceneAssetAsync<TAsset>(address);
        }

        public override void ReleaseAsset(ulong id)
        {
            if (!TryGetValidAddress(id, out string address))
                return;

            _resourceManager?.ReleaseSceneAsset(address);
        }

        private bool TryGetValidAddress(ulong id, out string address)
        {
            if (_runtimeAddressDict == null) InitializeRuntimeCache();

            return _runtimeAddressDict.TryGetValue(id, out address);
        }
    }
}