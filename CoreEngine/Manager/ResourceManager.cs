using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoreEngine.Manager
{
    /// <summary>
    /// Addressable 에셋 로드 및 메모리 관리를 전담하는 순수 범용 프레임워크 매니저
    /// </summary>
    public class ResourceManager : BaseManager
    {
        // 싱글톤 캐싱 (유저님의 Hub/Facade 환경에 맞춰 접근 방식을 변형하셔도 좋습니다)
        public static ResourceManager Inst { get; private set; }

        // 🌟 핵심: 제네릭 타입을 섞어서 보관하기 위해 비제네릭 AsyncOperationHandle 사용
        // 1. 공용 저장소 (게임 종료 시까지 유지)
        private Dictionary<string, AsyncOperationHandle> _globalHandles = new();

        // 2. 씬 전용 저장소 (씬 전환 시 비워짐)
        private Dictionary<string, AsyncOperationHandle> _sceneHandles = new();

        protected override void Awake()
        {
            base.Awake();
            Inst = this;
        }

        public override IEnumerator Initialize()
        {
            yield break;
        }

        public override void Exit()
        {
            ReleaseSceneAssets();
            ReleaseGlobalAssets();
        }

        // =========================================================
        // [비동기 로드 시스템] : 어떤 타입(T)이든 로드 가능
        // =========================================================

        /// <summary>
        /// 씬 전환 시 자동으로 해제될 에셋을 비동기로 로드합니다. (지도 타일, 씬 전용 UI 등)
        /// </summary>
        public void LoadSceneAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            LoadAssetInternal(address, _sceneHandles, onComplete);
        }

        /// <summary>
        /// 게임 종료 시까지 유지될 에셋을 비동기로 로드합니다. (공용 UI, 시스템 사운드 등)
        /// </summary>
        public void LoadGlobalAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            LoadAssetInternal(address, _globalHandles, onComplete);
        }

        private void LoadAssetInternal<T>(string address, Dictionary<string, AsyncOperationHandle> handleDict, Action<T> onComplete) where T : UnityEngine.Object
        {
            // 1. 이미 캐시에 존재하는지 검사 (중복 로드 방지)
            if (handleDict.TryGetValue(address, out AsyncOperationHandle existingHandle))
            {
                if (existingHandle.IsDone)
                {
                    // 로드가 끝난 상태면 즉시 콜백 반환
                    onComplete?.Invoke(existingHandle.Result as T);
                }
                else
                {
                    // 아직 로드 중이면 완료 이벤트에 대기열(콜백)만 추가
                    existingHandle.Completed += (op) => onComplete?.Invoke(op.Result as T);
                }
                return;
            }

            // 2. 캐시에 없다면 새로 로드 요청
            var newHandle = Addressables.LoadAssetAsync<T>(address);
            handleDict.Add(address, newHandle);

            newHandle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    Debug.LogError($"[ResourceManager] 에셋 로드 실패: {address}");
                    handleDict.Remove(address); // 실패 시 딕셔너리에서 지워 재시도 가능하게 함
                }
            };
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
    }
}