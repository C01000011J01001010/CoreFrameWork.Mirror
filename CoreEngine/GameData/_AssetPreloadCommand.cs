using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    // 테이블에 구워질 명령 베이스
    [Serializable]
    internal abstract class _AssetPreloadCommand
    {
        [SerializeField] protected int[] ids;
        public int[] Ids => ids;

        protected _AssetPreloadCommand() { }
        protected _AssetPreloadCommand(int[] ids) { this.ids = ids; }

        public abstract Task LoadAsync();
        public abstract void Release();
    }

    // 실제 제네릭 타입 정보를 담아 직렬화되는 자식 명령
    [Serializable]
    internal class AssetPreloadCommand<TAsset, TAssetRegistry> : _AssetPreloadCommand
        where TAsset : Object
        where TAssetRegistry : BaseAssetRegistry<TAsset>
    {
        //public readonly static Type RegistryType = typeof(TAssetRegistry);

        public AssetPreloadCommand(int[] ids) : base(ids) { }

        public override async Task LoadAsync()
        {
            if (ids == null || ids.Length == 0) return;

            var loadTasks = new List<Task>(ids.Length);
            foreach (int id in ids)
            {
                if (id > 0)
                {
                    loadTasks.Add(AssetRegistryRouter.LoadAsync(AssetId<TAsset, TAssetRegistry>.RegistryType, id));
                }
            }

            // 타입별로 묶인 ID들을 한 번에 병렬 로드 대기
            await Task.WhenAll(loadTasks);
        }

        public override void Release()
        {
            if (ids == null || ids.Length == 0) return;

            foreach (int id in ids)
            {
                if (id > 0)
                {
                    AssetRegistryRouter.Release(AssetId<TAsset, TAssetRegistry>.RegistryType, id);
                }
            }
        }
    }
}