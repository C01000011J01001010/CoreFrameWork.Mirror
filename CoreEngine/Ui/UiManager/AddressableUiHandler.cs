using CoreEngine.Extensions;
using CoreEngine.Facades;
using CoreEngine.Resource;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.UI
{
    /// <summary>
    /// Addressable UI의 로드 및 생명주기를 관리한다.
    /// </summary>
    public class AddressableUiHandler
    {
        // Handler가 현재 관리 중인 UI
        private readonly Dictionary<Type, IAddressableUi> _managedUis = new();

        // Address별 진행 중인 Load Task
        private readonly Dictionary<string, Task<IAddressableUi>> _loadingUis = new();

        // UI별 예약된 Release 취소 토큰
        private readonly Dictionary<Type, CancellationTokenSource> _releaseTokens = new();

        private ResourceManager _resourceManager;
        private IUiAddressRegistry _uiAddressRegistry;

        private bool _isReleased;

        public void Initialize()
        {
            _resourceManager = CoreFacade.GetManager<ResourceManager>();
            _uiAddressRegistry = CoreFacade.GetActor<IUiAddressRegistry>();

            _isReleased = false;
        }

        /// <summary>
        /// UI가 다시 표시될 때 예약된 Release를 취소한다.
        /// </summary>
        public void OnShow(Type uiType)
        {
            CancelRelease(uiType);
        }

        /// <summary>
        /// Addressable UI를 로드하여 반환한다.
        /// 이미 Handler가 관리하고 있다면 기존 UI를 반환한다.
        /// </summary>
        public async Task<IAddressableUi> Load(Type uiType)
        {
            if (_isReleased)
                return null;

            // 이미 관리 중이면 기존 UI 반환
            if (_managedUis.TryGetValue(uiType, out var managedUi))
            {
                return managedUi;
            }

            string address = _uiAddressRegistry.GetAddress(uiType);

            if (string.IsNullOrEmpty(address))
                return null;

            // 같은 Address를 이미 로드 중이면 해당 Task 공유
            if (_loadingUis.TryGetValue(address, out var OldLoadingTask))
            {
                return await OldLoadingTask;
            }

            var newLoadingTask = LoadInternal(uiType, address);
            _loadingUis.Add(address, newLoadingTask);

            try
            {
                return await newLoadingTask;
            }
            finally
            {
                _loadingUis.Remove(address);
            }
        }
        //private async Task<IAddressableUi> GetLoadResult(Task<IAddressableUi> loadingTask, string address)
        //{
        //    var result = await loadingTask;
        //    return _isReleased ? null : result;
        //}

        private async Task<IAddressableUi> LoadInternal(Type uiType, string address)
        {
            GameObject loadedUiObj =
                await _resourceManager.LoadSceneAssetAsync<GameObject>(address);

            // Exit 이후 Load가 완료된 경우
            if (_isReleased)
            {
                if (loadedUiObj != null) _resourceManager.ReleaseSceneAsset(address);
                return null;
            }

            // Load 실패
            if (loadedUiObj == null) return null;

            // Addressable UI가 아니면 Asset Release
            if (!loadedUiObj.TryGetComponent(out IAddressableUi loadedUi))
            {
                _resourceManager.ReleaseSceneAsset(address);
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(loadedUiObj);

            // 현재 씬과 생명주기를 같이함
            // Global Scene에서 관리될 경우 UiManager는 죽는데 Ui는 살아있는
            // 유령객체 문제가 생길 수 있음
            instance.MoveScene(CoreFacade.GetCurrentScene());

            // 원본 Prefab Asset은 더 이상 필요하지 않음
            _resourceManager.ReleaseSceneAsset(address);

            // Instance에서 UI Component 획득
            if (!instance.TryGetComponent<IAddressableUi>(out var UiInst))
            {
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            _managedUis[uiType] = UiInst;

            return UiInst;
        }

        /// <summary>
        /// Addressable UI를 숨긴다.
        /// ReleaseDelay 정책에 따라 즉시 또는 지연 해제한다.
        /// </summary>
        public bool OnHide(Type uiType)
        {
            if (!_managedUis.TryGetValue(uiType, out var managedUi))
                return false;

            // 기존 Release 예약이 있다면 취소
            CancelRelease(uiType);

            ScheduleRelease(uiType, managedUi);

            return true;
        }

        /// <summary>
        /// 특정 Addressable UI를 즉시 Release한다.
        /// </summary>
        public bool Release(Type uiType)
        {
            // 예약된 Release가 있다면 취소
            CancelRelease(uiType);

            if (!_managedUis.TryGetValue(uiType, out var managedUi))
                return false;

            string address = _uiAddressRegistry.GetAddress(managedUi);
            if (string.IsNullOrEmpty(address))
                return false;

            _managedUis.Remove(uiType);
            UnityEngine.Object.Destroy(managedUi.gameObject);
            //_resourceManager.ReleaseSceneAsset(address);

            return true;
        }

        /// <summary>
        /// Handler가 관리하고 있는 모든 Addressable UI를 Release한다.
        /// </summary>
        public void ReleaseAll()
        {
            _isReleased = true;

            // 예약된 Release 전부 취소
            foreach (var cts in _releaseTokens.Values)
            {
                cts.Cancel();
            }

            _releaseTokens.Clear();

            // 관리 중인 UI 전부 Release
            foreach (var managedUi in _managedUis.Values)
            {
                string address = _uiAddressRegistry.GetAddress(managedUi);

                if (!string.IsNullOrEmpty(address))
                {
                    _resourceManager.ReleaseSceneAsset(address);
                }
            }

            _managedUis.Clear();
        }

        private void ScheduleRelease(Type uiType, IAddressableUi managedUi)
        {
            float delay = managedUi.ReleaseDelay;

            // -1 이하 : 계속 유지
            if (delay < 0f)
                return;

            // 0 : 즉시 Release
            if (delay == 0f)
            {
                Release(uiType);
                return;
            }

            var cts = new CancellationTokenSource();
            _releaseTokens[uiType] = cts;

            _ = DelayedRelease(
                uiType,
                managedUi,
                delay,
                cts);
        }

        private async Task DelayedRelease(
            Type uiType,
            IAddressableUi managedUi,
            float delay,
            CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(delay),
                    cts.Token);

                if (_isReleased)
                    return;

                // 같은 UI 객체가 아직 관리되고 있는지 확인
                if (_managedUis.TryGetValue(uiType, out var current) &&
                    ReferenceEquals(current, managedUi))
                {
                    Release(uiType);
                }
            }
            catch (OperationCanceledException)
            {
                // Show 또는 다른 Release에 의해 정상적으로 취소됨
            }
            finally
            {
                // 새로운 Release 예약이 이미 만들어졌다면 제거하지 않는다.
                if (_releaseTokens.TryGetValue(uiType, out var currentCts) &&
                    ReferenceEquals(currentCts, cts))
                {
                    _releaseTokens.Remove(uiType);
                }

                cts.Dispose();
            }
        }

        private void CancelRelease(Type uiType)
        {
            if (_releaseTokens.TryGetValue(uiType, out var cts))
            {
                // 여기서는 Dispose하지 않는다.
                // 실제 Dispose는 DelayedRelease의 finally에서 담당한다.
                cts.Cancel();
            }
        }
    }
}