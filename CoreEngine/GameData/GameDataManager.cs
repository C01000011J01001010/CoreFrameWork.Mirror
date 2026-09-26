using CoreEngine.Facades;
using CoreEngine.Manager;
using CoreEngine.Resource;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.GameData
{
    /// <summary>
    /// 게임이 시작할 때 "GlobalData" 라벨을 가진 모든 Table과 Registry를 한 번에 로드하고 보관하는 매니저
    /// </summary>
    public class GameDataManager : BaseManager, IPriority
    {
        // ResourceManager(Infrastructure) 이후에 초기화되어야 하므로 동일하거나 약간 후순위로 설정
        public int Priority => (int)ManagerPriority.Infrastructure;

        [Header("게임 시작시 로드할 데이터들의 라벨 지정"),SerializeField]
        private string _label = "Table And Registry"; // 기본 라벨

        // 로드된 테이블과 레지스트리를 타입별로 보관할 딕셔너리
        private readonly Dictionary<Type, _DataTable> _tables = new();
        private readonly Dictionary<Type, _AssetRegistry> _registries = new();

        // Hub에 의해 처음 초기화 될 때
        protected override IEnumerator OnInitialize()
        {
            var resourceManager = CoreFacade.GetManager<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("[GlobalDataManager] ResourceManager를 찾을 수 없어 데이터를 로드할 수 없습니다.");
                yield break;
            }

            bool isLoaded = false;

            // ResourceManager를 통해 GlobalData 라벨을 가진 모든 SO를 비동기 로드
            resourceManager.LoadGlobalAssetsByLabelAsync<ScriptableObject>(_label, (assets) =>
            {
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        if (asset is _DataTable table)
                        {
                            Type tableType = table.GetType();

                            // [중복 검사] 이미 같은 타입의 테이블이 등록되어 있다면?
                            if (_tables.ContainsKey(tableType))
                            {
                                Debug.LogError($"[GlobalDataManager] 치명적 에러: {tableType.Name} 타입의 테이블 SO가 중복 발견되었습니다! '{asset.name}' 에셋을 확인하세요.");
                                continue; // 덮어씌우지 않고 스킵
                            }

                            _tables.Add(tableType, table);
                        }
                        else if (asset is _AssetRegistry registry)
                        {
                            Type registryType = registry.GetType();

                            // [중복 검사] 이미 같은 타입의 레지스트리가 등록되어 있다면?
                            if (_registries.ContainsKey(registryType))
                            {
                                Debug.LogError($"[GlobalDataManager] 치명적 에러: {registryType.Name} 타입의 레지스트리 SO가 중복 발견되었습니다! '{asset.name}' 에셋을 확인하세요.");
                                continue; // 덮어씌우지 않고 스킵
                            }

                            _registries.Add(registryType, registry);
                            registry.InitializeRuntimeCache();
                        }
                    }
                }

                isLoaded = true;
            });

            // 콜백이 완료될 때까지 Hub의 초기화 시퀀스를 대기시킵니다 (매우 중요!)
            yield return new WaitUntil(() => isLoaded);

            yield return base.OnInitialize();
        }

        // Hub에 의해 메모리가 정리 될 때
        public override void OnExit()
        {
            // 딕셔너리 참조 해제
            _tables.Clear();
            _registries.Clear();

            // 실제 메모리 릴리즈는 ResourceManager의 OnExit()에서 일괄(ReleaseGlobalAssets) 처리되므로
            // 여기서는 C# 레퍼런스만 비워주면 됩니다.

            base.OnExit();
        }

        // =========================================================
        // [Public API] 데이터 제공 인터페이스
        // =========================================================

        public TTable GetTable<TTable>() where TTable : _DataTable
        {
            if (_tables.TryGetValue(typeof(TTable), out _DataTable table))
            {
                return table as TTable;
            }
            Debug.LogWarning($"[GlobalDataManager] {typeof(TTable).Name} 테이블을 찾을 수 없습니다.");
            return null;
        }

        public TRegistry GetRegistry<TRegistry>() where TRegistry : _AssetRegistry
        {
            if (_registries.TryGetValue(typeof(TRegistry), out _AssetRegistry registry))
            {
                return registry as TRegistry;
            }
            Debug.LogWarning($"[GlobalDataManager] {typeof(TRegistry).Name} 레지스트리를 찾을 수 없습니다.");
            return null;
        }
    }
}