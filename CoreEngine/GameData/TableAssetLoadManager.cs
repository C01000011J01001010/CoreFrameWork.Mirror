using CoreEngine.Facades;
using CoreEngine.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    /// <summary>
    /// 로딩 씬에서 호출되어, 씬 전환 시 에셋의 델타 로딩(차집합 비교)을 수행합니다.
    /// </summary>
    public class TableAssetLoadManager : BaseManager
    {
        // 씬이 전환되어 이 매니저가 파괴되었다가 다시 생성되어도 
        // 이전 씬의 로드 상태를 기억해야 하므로 static을 유지합니다.
        private static readonly HashSet<Type> _activeTables = new HashSet<Type>();

        // TODO: 각 씬마다 어떤 테이블을 로드할지 외부(씬 컨텍스트 등)에서 주입해주어야 합니다.
        private List<Type> _requestedTableTypes = new List<Type>();

        private GameDataManager _gameDataManager;

        protected override IEnumerator OnInitialize()
        {
            _gameDataManager = CoreFacade.GetManager<GameDataManager>();
            if (_gameDataManager == null)
            {
                Debug.LogError("[TableAssetLoadManager] GameDataManager를 찾을 수 없습니다.");
                yield break;
            }

            // 비동기 델타 로딩 실행
            var task = UpdateTableAssetsAsync(_requestedTableTypes);

            // Task가 완전히 끝날 때까지 코루틴 대기
            yield return new WaitUntil(() => task.IsCompleted);
        }

        /// <summary>
        /// static을 제거하여 인스턴스로 캐싱된 _gameDataManager를 안전하게 사용합니다.
        /// </summary>
        private async Task UpdateTableAssetsAsync(List<Type> requestedTypes)
        {
            var requestedSet = new HashSet<Type>(requestedTypes);
            var toRelease = new List<Type>();
            var toLoad = new List<Type>();

            // 1. Release 대상 추출 (기존엔 있었지만 새 씬엔 없는 것)
            foreach (var activeType in _activeTables)
            {
                if (!requestedSet.Contains(activeType))
                    toRelease.Add(activeType);
            }

            // 2. Load 대상 추출 (새 씬에 필요하지만 기존엔 없는 것)
            foreach (var reqType in requestedSet)
            {
                if (!_activeTables.Contains(reqType))
                    toLoad.Add(reqType);
            }

            // 3. 릴리즈 즉시 실행
            foreach (var type in toRelease)
            {
                // [수정 완료] 싱글톤 호출 제거, Facade로 찾은 매니저 인스턴스 사용
                var table = _gameDataManager.GetTable(type);
                if (table != null)
                {
                    table.ReleaseAssets();
                }

                _activeTables.Remove(type);
            }

            // 4. 로드 비동기 실행
            var loadTasks = new List<Task>();
            foreach (var type in toLoad)
            {
                var table = _gameDataManager.GetTable(type);
                if (table != null)
                {
                    loadTasks.Add(table.PreloadAssetsAsync());
                    _activeTables.Add(type);
                }
            }

            // 5. 새롭게 요구된 에셋들이 모두 로드될 때까지 병렬 대기
            if (loadTasks.Count > 0)
            {
                await Task.WhenAll(loadTasks);
            }
        }
    }
}