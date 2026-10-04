using CoreEngine.Facades;
using CoreEngine.Helpers;
using CoreEngine.Manager;
using CoreEngine.Resource;
using CoreEngine.Settings;
using System;
using System.Collections;
using UnityEngine;

namespace CoreEngine.GameData
{
    public class GameDataManager : BaseManager, IPriority
    {
        public int Priority => (int)ManagerPriority.StaticData;
        private string Label => CoreEngineAutoSettingsSO.Instance.GameDataLabel;
        private const int DestroyDelay = 5;

        protected override IEnumerator OnInitialize()
        {
            var resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("[GameDataManager] ResourceManager 누락.");
                yield break;
            }

            bool isLoaded = false;

            resourceManager.LoadGlobalAssetsByLabelAsync<ScriptableObject>(Label, (assets) =>
            {
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        Type assetType = asset.GetType();

                        // 내부 dictionary로 중복 등록 차단
                        if (asset is _Table table)
                        {
                            TableRouter.InjectTable(table);
                        }
                        else if (asset is _AssetRegistry registry)
                        {
                            AssetRegistryRouter.InjectRegistry(registry);
                        }
                    }
                }
                isLoaded = true;
            });

            yield return new WaitUntil(() => isLoaded);
            yield return base.OnInitialize();

            LogHelper.Log($"역할이 끝난 GameDataManager를 제거하겠습니다.\n" +
                $"(안전하게 {DestroyDelay}초 지연)", LogColor.Green);
            Invoke(nameof(DestroyThis), DestroyDelay);
        }

        private void DestroyThis()
        {
            Destroy(this);
        }
    }
}