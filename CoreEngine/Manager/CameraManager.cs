using CoreEngine.EventBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreEngine.Manager
{
    #region Camera Events
    public struct RegisterVirtualCameraEvent : IEvent
    {
        public readonly VirtualCameraController Camera;
        public readonly bool IsRegister;

        public RegisterVirtualCameraEvent(VirtualCameraController camera, bool isRegister)
        {
            Camera = camera;
            IsRegister = isRegister;
        }
    }

    public struct SwitchCameraEvent : IEvent
    {
        public readonly Type TargetCameraType;
        public readonly Func<VirtualCameraController, bool> Predicate; // 카메라 선별 조건

        public SwitchCameraEvent(Type targetCameraType, Func<VirtualCameraController, bool> predicate = null)
        {
            TargetCameraType = targetCameraType;
            Predicate = predicate;
        }
    }

    public struct SetCameraTargetEvent : IEvent
    {
        public readonly Transform target;
        public readonly Type targetCameraType; // null이면 모든 카메라, 특정 타입이 있으면 해당 카메라만 타겟 변경

        public SetCameraTargetEvent(Transform target, Type targetCameraType = null)
        {
            this.target = target;
            this.targetCameraType = targetCameraType;
        }
    }
    #endregion

    public class CameraManager : BaseManager, ILateTickable
    {

        protected MainCameraController _mainCamera;

        protected Dictionary<Type, HashSet<VirtualCameraController>> _virtualCameras = new();

        // 지각하는 카메라를 위한 예약 슬롯
        private SwitchCameraEvent? _pendingRequest = null;

        protected VirtualCameraController _currentCamera;

        // 이벤트 발행자가 먼저 발행하는 경우를 대비해서 RepeatEventConsumer를 사용하여 이벤트를 구독함
        protected RepeatEventConsumer<SwitchCameraEvent> _repeatEventConsumer;

        // ILateTickable 구현 (UpdateManager의 통제를 받음)
        public LateTickGroup LateTickGroup => LateTickGroup.Camera;

        public override IEnumerator Initialize()
        {
            _mainCamera = GetComponentInChildren<MainCameraController>();

            // 이벤트 구독 (등록, 전환, 옵션변경 등)
            EventBus<RegisterVirtualCameraEvent>.Subscribe(OnVirtualCameraRegistered);

            _repeatEventConsumer = new RepeatEventConsumer<SwitchCameraEvent>(SwitchCamera);
            _repeatEventConsumer.Bind();

            if (_mainCamera != null)
                yield return _mainCamera.Initialize();
        }

        public override void Exit()
        {
            EventBus<RegisterVirtualCameraEvent>.Unsubscribe(OnVirtualCameraRegistered);
            _repeatEventConsumer.Unbind();
        }

        public void LateTick(float dt)
        {
            // 현재 활성화된 카메라의 커스텀 로직만 실행 (최적화)
            _currentCamera?.CameraTick(dt);
        }

        private void OnVirtualCameraRegistered(RegisterVirtualCameraEvent evt)
        {
            Type camType = evt.Camera.GetType();

            if (evt.IsRegister)
            {
                // Type키가 없다면 생성
                if (!_virtualCameras.ContainsKey(camType))
                    _virtualCameras.Add(camType, new HashSet<VirtualCameraController>());

                _virtualCameras[camType].Add(evt.Camera);

                // 예약된 카메라가 뒤늦게 출근했다면 즉시 렌즈를 넘김
                if (_pendingRequest.HasValue && 
                    _pendingRequest.Value.TargetCameraType == camType)
                {
                    var predicate = _pendingRequest.Value.Predicate;

                    // 조건이 없거나, 새 카메라가 조건을 만족한다면 즉시 렌즈 전환!
                    if (predicate == null || predicate(evt.Camera))
                    {
                        SwitchCamera(_pendingRequest.Value);
                        _pendingRequest = null; // 예약 처리 완료
                    }
                }
            }
            else
            {
                if (_virtualCameras.ContainsKey(camType))
                {
                    _virtualCameras[camType].Remove(evt.Camera);
                }

                // 사용하던 카메라가 사라졌으니 null 처리
                if (_currentCamera == evt.Camera)
                {
                    _currentCamera = null;
                }
            }
        }

        /// <summary>
        /// 특정 타입의 가상 카메라를 활성화 (Type 기반)
        /// </summary>
        protected void SwitchCamera(SwitchCameraEvent evt)
        {
            if (_virtualCameras.TryGetValue(evt.TargetCameraType, out var cameras) && cameras.Count > 0)
            {
                VirtualCameraController targetCam = null;

                if (evt.Predicate != null)
                {
                    // LINQ를 활용하여 조건에 맞는 첫 번째 카메라 선별
                    targetCam = cameras.FirstOrDefault(evt.Predicate);
                }

                // 조건이 없거나 조건에 맞는 카메라를 못 찾았다면, 집합의 가장 첫 번째 객체 반환
                if (targetCam == null)
                {
                    targetCam = cameras.First();
                }

                SetActiveCamera(targetCam);

                // 현재 필요한 카메라 세팅에 성공했으니 낡은 예약은 취소됨
                if (_pendingRequest.HasValue)
                    _pendingRequest = null;
            }
            else
            {
                // 씬에 해당 타입의 카메라가 하나도 없다면 요청을 통째로 예약
                _pendingRequest = evt;
            }
        }

        public T GetCurrentCamera<T>() where T : VirtualCameraController
        {
            if (_currentCamera is T matched) return matched;

            Debug.LogError($"현재 활성 카메라({_currentCamera?.GetType().Name})가 요청하신 {typeof(T).Name}와 다릅니다.");
            return null;
        }

        private void SetActiveCamera(VirtualCameraController newCamera)
        {
            var oldCamera = _currentCamera;
            if (oldCamera == newCamera) return;

            // 비활성화 (GameObject를 끄지 않고 Priority를 0으로)
            if (oldCamera != null)
                oldCamera.SetActive(false);

            // 활성화 (Priority를 10으로 올려서 렌즈를 가져옴)
            newCamera.SetActive(true);
            _currentCamera = newCamera;
        }
    }
}