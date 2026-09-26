using CoreEngine.DesignPattern.Singleton;
using CoreEngine.Facades;
using CoreEngine.Resource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    // 에디터 윈도우나 포스트프로세서에서 모든 타입의 레지스트리를 
    // 일괄적으로 찾고 관리하기 위한 마커 클래스입니다
    public abstract class _AssetRegistry : ScriptableObject
    {
        // GlobalDataManager가 호출할 공통 인터페이스
        public abstract void InitializeRuntimeCache();
    }

    public class BaseAssetRegistry<TAsset> : _AssetRegistry
        where TAsset : UnityEngine.Object
    {
        [Serializable]
        public struct AssetEntry
        {
            public int Id;
            public string PathDetail; // _baseDirectory 이후로의 경로와 파일이름 확장자
        }

        [SerializeField] private string _baseDirectory;
        [SerializeField] private List<AssetEntry> _entries = new List<AssetEntry>();

        private Dictionary<int, string> _runtimeAddressDict;

        public override void InitializeRuntimeCache()
        {
            _runtimeAddressDict = new Dictionary<int, string>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (entry.Id > 0)
                {
                    _runtimeAddressDict.Add(entry.Id, $"{_baseDirectory}/{entry.PathDetail}");
                }
            }
        }

        // ==========================================
        // 1. 동기 조회 (즉시 반환 또는 강제 로드)
        // ==========================================
        public TAsset GetAsset(int id)
        {
            if (_runtimeAddressDict == null || !_runtimeAddressDict.TryGetValue(id, out string address))
                return null;

            // Facade를 통해 ResourceManager를 찾아 안전하게 로드합니다.
            var resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (resourceManager == null) return null;

            return resourceManager.LoadSceneAssetSync<TAsset>(address);
        }

        // ==========================================
        // 2. 비동기 사전 로드 (로딩 화면용)
        // ==========================================
        public Task<TAsset> LoadAssetAsync(int id)
        {
            if (_runtimeAddressDict == null || !_runtimeAddressDict.TryGetValue(id, out string address))
                return Task.FromResult<TAsset>(null);

            var resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (resourceManager == null) return Task.FromResult<TAsset>(null);

            // ResourceManager에 만들어두신 Task 반환 메서드를 호출합니다.
            return resourceManager.LoadSceneAssetAsync<TAsset>(address);
        }
    }
}