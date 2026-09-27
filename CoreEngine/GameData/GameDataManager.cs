using CoreEngine.Facades;
using CoreEngine.Manager;
using CoreEngine.Resource;
using CoreEngine.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.GameData
{
    public class GameDataManager : BaseManager, IPriority
    {
        public int Priority => (int)ManagerPriority.StaticData;

        private string Label => CoreEngineAutoSettingsSO.Instance.GameDataLabel;

        // [최적화] 레지스트리는 AssetRouter가 보관하므로 매니저에서 들고 있을 필요가 없습니다. (테이블만 보관)
        private readonly Dictionary<Type, _DataTable> _tables = new();

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
                    // 최적화된 초기화 방어용 해시셋 (중복 로드 방지)
                    HashSet<Type> initializedRegistries = new HashSet<Type>();

                    foreach (var asset in assets)
                    {
                        if (asset is _DataTable table)
                        {
                            Type tableType = table.GetType();
                            if (!_tables.TryAdd(tableType, table))
                            {
                                Debug.LogError($"[GameDataManager] 테이블 중복: {tableType.Name}");
                            }
                        }
                        else if (asset is _AssetRegistry registry)
                        {
                            Type registryType = registry.GetType();

                            // 레지스트리는 저장하지 않고 Router에 1회성으로 주입하고 끝냅니다.
                            if (initializedRegistries.Add(registryType))
                            {
                                registry.InitializeRuntimeCache();
                                InjectRegistryToRouter(registry);
                            }
                            else
                            {
                                Debug.LogError($"[GameDataManager] 레지스트리 중복: {registryType.Name}");
                            }
                        }
                    }
                }
                isLoaded = true;
            });

            yield return new WaitUntil(() => isLoaded);
            yield return base.OnInitialize();
        }

        public override void OnExit()
        {
            _tables.Clear();
            base.OnExit();
        }

        // =========================================================
        // [내부 헬퍼] AssetRouter 동적 주입 로직
        // =========================================================
        private void InjectRegistryToRouter(_AssetRegistry registry)
        {
            var assetType = GetAssetTypeFromRegistry(registry.GetType());
            if (assetType != null)
            {
                var routerType = typeof(AssetRouter<>).MakeGenericType(assetType);
                var injectMethod = routerType.GetMethod("InjectRegistry", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                injectMethod?.Invoke(null, new object[] { registry });
            }
        }

        private Type GetAssetTypeFromRegistry(Type type)
        {
            while (type != null && type != typeof(ScriptableObject))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BaseAssetRegistry<>))
                    return type.GetGenericArguments()[0];
                type = type.BaseType;
            }
            return null;
        }

        // =========================================================
        // [Public API] 데이터 제공 인터페이스
        // =========================================================

        // [수정] GetRegistry<T> 삭제. 외부에서는 철저히 AssetId.Get() 만 사용하도록 통제

        public TTable GetTable<TTable>() where TTable : _DataTable
        {
            if (_tables.TryGetValue(typeof(TTable), out _DataTable table))
                return table as TTable;

            Debug.LogWarning($"[GameDataManager] {typeof(TTable).Name} 테이블 누락.");
            return null;
        }

        // [추가] TableAssetLoadManager가 타입(Type) 객체로 직접 테이블을 찾기 위한 오버로딩
        public _DataTable GetTable(Type type)
        {
            if (_tables.TryGetValue(type, out _DataTable table))
                return table;

            Debug.LogWarning($"[GameDataManager] {type.Name} 테이블 누락.");
            return null;
        }
    }
}