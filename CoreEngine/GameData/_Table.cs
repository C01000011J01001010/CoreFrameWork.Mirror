using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    internal interface IPreloadAssetIdsBaker
    {
#if UNITY_EDITOR
        void BakePreloadCommands(_AssetPreloadCommand[] bakedIds);
#endif
    }
    public abstract class _Table : ScriptableObject, IPreloadAssetIdsBaker
    {
        // [핵심] SerializeReference를 통해 자식 제네릭 클래스들의 다형성을 유지하며 저장
        [SerializeReference, ReadOnly]
        private _AssetPreloadCommand[] _preloadCommands;

        public abstract void InitializeRuntimeCache();

        public abstract IRecord GetRecord(int id);

        // ==========================================
        // [런타임 전용] TableAssetLoadManager가 호출할 API
        // ==========================================
        internal async Task PreloadAssetsAsync()
        {
            if (_preloadCommands == null || _preloadCommands.Length == 0) return;

            var loadTasks = new List<Task>(_preloadCommands.Length);
            foreach (var cmd in _preloadCommands)
            {
                if (cmd != null)
                {
                    loadTasks.Add(cmd.LoadAsync());
                }
            }
            await Task.WhenAll(loadTasks);
        }

        internal void ReleaseAssets()
        {
            if (_preloadCommands == null || _preloadCommands.Length == 0) return;

            foreach (var cmd in _preloadCommands)
            {
                if (cmd != null)
                {
                    cmd.Release();
                }
            }
        }


#if UNITY_EDITOR
        void IPreloadAssetIdsBaker.BakePreloadCommands(_AssetPreloadCommand[] bakedIds)
        {
            _preloadCommands = bakedIds;
        }
#endif
    }
}