using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    public static class AssetRegistryRouter
    {
        private readonly static Dictionary<Type, _AssetRegistry> _registryMap = new();

        // GameDataManager가 어드레서블로 로드한 뒤 이 메서드를 호출해 주입
        public static void InjectRegistry(_AssetRegistry registry)
        {
            if(_registryMap.ContainsKey(registry.GetType()))
            {
                Debug.LogWarning($"Registry of type {registry.GetType().Name} is already injected.");
                return;
            }
            _registryMap[registry.GetType()] = registry;
            registry.InitializeRuntimeCache();
        }

        public static Object Get(Type registryType, ulong id)
        {
            if(_registryMap.TryGetValue(registryType, out _AssetRegistry registry))
            {
                return registry.GetAsset(id);
            }
            return null;
        }

        public static Task<Object> LoadAsync(Type registryType, ulong id)
        {
            if(_registryMap.TryGetValue(registryType, out _AssetRegistry registry))
            {
                return registry.LoadAssetAsync(id);
            }
            return Task.FromResult<Object>(null);
        }

        public static void Release(Type registryType, ulong id)
        {
            if(_registryMap.TryGetValue(registryType, out _AssetRegistry registry))
            {
                registry.ReleaseAsset(id);
            }
        }
    }
}