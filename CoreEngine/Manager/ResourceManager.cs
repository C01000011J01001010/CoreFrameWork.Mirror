using CoreEngine.EventBus;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoreEngine.Manager
{
    /// <summary>
    /// Addressable 에셋 로드 및 메모리 관리를 전담하는 순수 범용 프레임워크 매니저
    /// </summary>
    public class ResourceManager : BaseManager, IPriority
    {
        public int Priority => (int)ManagerPriority.Infrastructure;

        private readonly Dictionary<string, AsyncOperationHandle> _globalHandles = new();
        private readonly Dictionary<string, AsyncOperationHandle> _sceneHandles = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus<SceneLoadRequestEvent>.Subscribe(OnLoadSceneRequset);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<SceneLoadRequestEvent>.Unsubscribe(OnLoadSceneRequset);
        }

        public override void Exit()
        {
            ReleaseSceneAssets();
            ReleaseGlobalAssets();
        }

        // =========================================================
        // [Public API]
        // =========================================================

        public void LoadSceneAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            string cacheKey = $"Addr_{address}";
            ProcessHandleLoad(
                cacheKey,
                _sceneHandles,
                () => Addressables.LoadAssetAsync<T>(address),
                (handle) => InvokeCallbackSafely(handle, onComplete),
                address
            );
        }

        public void LoadGlobalAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            string cacheKey = $"Addr_{address}";
            ProcessHandleLoad(
                cacheKey,
                _globalHandles,
                () => Addressables.LoadAssetAsync<T>(address),
                (handle) => InvokeCallbackSafely(handle, onComplete),
                address
            );
        }

        public void LoadSceneAssetsByLabelAsync<T>(string label, Action<IList<T>> onComplete) where T : UnityEngine.Object
        {
            string cacheKey = $"Label_{label}";
            ProcessHandleLoad(
                cacheKey,
                _sceneHandles,
                () => Addressables.LoadAssetsAsync<T>(label, null),
                (handle) => InvokeLabelCallbackSafely(handle, onComplete),
                label
            );
        }

        public void LoadGlobalAssetsByLabelAsync<T>(string label, Action<IList<T>> onComplete) where T : UnityEngine.Object
        {
            string cacheKey = $"Label_{label}";
            ProcessHandleLoad(
                cacheKey,
                _globalHandles,
                () => Addressables.LoadAssetsAsync<T>(label, null),
                (handle) => InvokeLabelCallbackSafely(handle, onComplete),
                label
            );
        }

        // =========================================================
        // [통합 비동기 생명주기 제어 코어]
        // =========================================================

        /// <summary>
        /// 단일/다중 로드 방식에 관계없이 캐시 검사 및 비동기 콜백 처리를 전담하는 공통 함수
        /// </summary>
        private void ProcessHandleLoad(
            string cacheKey,
            Dictionary<string, AsyncOperationHandle> handleDict,
            Func<AsyncOperationHandle> loadFunc,
            Action<AsyncOperationHandle> onResult,
            string logIdentifier)
        {
            // 이미 캐시에 존재하는지 검사 (중복 로드 방지)
            if (handleDict.TryGetValue(cacheKey, out AsyncOperationHandle existingHandle))
            {
                if (existingHandle.IsDone)
                {
                    onResult?.Invoke(existingHandle);
                }
                else
                {
                    existingHandle.Completed += (op) =>
                    {
                        if (handleDict.ContainsKey(cacheKey))
                        {
                            onResult?.Invoke(op);
                        }
                    };
                }
                return;
            }

            // 전달받은 팩토리 함수(loadFunc)로 비동기 작업 시작
            var newHandle = loadFunc.Invoke();
            handleDict.Add(cacheKey, newHandle);

            newHandle.Completed += (op) =>
            {
                // 로드 대기 중 Release가 발생했는지 검사
                if (!handleDict.ContainsKey(cacheKey))
                {
                    Debug.LogWarning($"[ResourceManager] 로드 완료 전 해제되었습니다: {logIdentifier}");
                    return;
                }

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onResult?.Invoke(op);
                }
                else
                {
                    Debug.LogError($"[ResourceManager] 에셋 로드 실패: {logIdentifier}");
                    handleDict.Remove(cacheKey);
                }
            };
        }

        // =========================================================
        // [안전한 콜백 파싱 헬퍼]
        // =========================================================

        private void InvokeCallbackSafely<T>(AsyncOperationHandle handle, Action<T> onComplete) where T : UnityEngine.Object
        {
            if (handle.Result is T resultAsset)
            {
                onComplete?.Invoke(resultAsset);
            }
            else
            {
                Debug.LogError($"[ResourceManager] 타입 불일치! 요청 타입: {typeof(T).Name}, 실제 타입: {handle.Result?.GetType().Name ?? "null"}");
                onComplete?.Invoke(null);
            }
        }

        private void InvokeLabelCallbackSafely<T>(AsyncOperationHandle handle, Action<IList<T>> onComplete) where T : UnityEngine.Object
        {
            if (handle.Result is IList<T> resultList)
            {
                onComplete?.Invoke(resultList);
            }
            else
            {
                Debug.LogError($"[ResourceManager] 라벨 리스트 타입 불일치! 요청 타입: {typeof(T).Name}");
                onComplete?.Invoke(null);
            }
        }

        // =========================================================
        // [메모리 해제 로직]
        // =========================================================

        public void ReleaseSceneAssets()
        {
            foreach (var handle in _sceneHandles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            _sceneHandles.Clear();
            Debug.Log("[ResourceManager] 씬 전용 에셋 메모리 해제 완료");
        }

        public void ReleaseGlobalAssets()
        {
            foreach (var handle in _globalHandles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            _globalHandles.Clear();
            Debug.Log("[ResourceManager] 공용 에셋 메모리 해제 완료");
        }

        // 새로운 씬 로드시 이전 씬은 필요없음
        private void OnLoadSceneRequset(SceneLoadRequestEvent evt)
        {
            ReleaseSceneAssets();
        }
    }
}