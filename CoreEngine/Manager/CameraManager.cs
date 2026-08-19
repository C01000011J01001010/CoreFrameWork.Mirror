using CoreEngine;
using CoreEngine.EventBus;
using CoreEngine.Manager;
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
        public VirtualCameraController Camera;
        public bool IsRegister;

        public RegisterVirtualCameraEvent(VirtualCameraController camera, bool isRegister)
        {
            Camera = camera;
            IsRegister = isRegister;
        }
    }

    public struct SwitchCameraEvent : IEvent
    {
        public Type TargetCameraType;

        public SwitchCameraEvent(Type targetCameraType)
        {
            TargetCameraType = targetCameraType;
        }
    }

    public struct SetCameraTargetEvent : IEvent
    {
        public Transform target;
        public Type targetCameraType; // null이면 모든 카메라, 특정 타입이 있으면 해당 카메라만 타겟 변경

        public SetCameraTargetEvent(Transform target, Type targetCameraType = null)
        {
            this.target = target;
            this.targetCameraType = targetCameraType;
        }
    }
    #endregion

    public class CameraManager : BaseManager, ILateTickable
    {
        MainCameraController _mainCamera;

        private List<VirtualCameraController> _virtualCameras = new();
        private VirtualCameraController _currentCamera;

        // ILateTickable 구현 (UpdateManager의 통제를 받음)
        public LateTickGroup LateTickGroup => LateTickGroup.Camera;

        public override IEnumerator Initialize()
        {
            _mainCamera = GetComponentInChildren<MainCameraController>();

            // 이벤트 구독 (등록, 전환, 옵션변경 등)
            EventBus<RegisterVirtualCameraEvent>.Subscribe(OnVirtualCameraRegistered);
            EventBus<SwitchCameraEvent>.Subscribe(OnSwitchCameraRequested);

            if (_mainCamera != null)
                yield return _mainCamera.Initialize();
        }

        public override void Exit()
        {
            EventBus<RegisterVirtualCameraEvent>.Unsubscribe(OnVirtualCameraRegistered);
            EventBus<SwitchCameraEvent>.Unsubscribe(OnSwitchCameraRequested);
        }

        public void LateTick(float dt)
        {
            // 현재 활성화된 카메라의 커스텀 로직만 실행 (최적화)
            _currentCamera?.CameraTick(dt);
        }

        private void OnVirtualCameraRegistered(RegisterVirtualCameraEvent evt)
        {
            if (evt.IsRegister && !_virtualCameras.Contains(evt.Camera))
            {
                _virtualCameras.Add(evt.Camera);
            }
            else if (!evt.IsRegister)
            {
                _virtualCameras.Remove(evt.Camera);
            }
        }

        private void OnSwitchCameraRequested(SwitchCameraEvent evt)
        {
            SwitchCamera(evt.TargetCameraType);
        }

        /// <summary>
        /// 특정 타입의 가상 카메라를 활성화 (Type 기반)
        /// </summary>
        public VirtualCameraController SwitchCamera(Type cameraType)
        {
            var target = _virtualCameras.FirstOrDefault(vcam => vcam.GetType() == cameraType);

            if (target != null)
            {
                SetActiveCamera(target);
                return target;
            }

            Debug.LogWarning($"VirtualCamera of type {cameraType}가 존재하지 않습니다.");
            return null;
        }

        /// <summary>
        /// 특정 타입의 가상 카메라를 활성화 (제네릭 기반 편의성 메서드)
        /// </summary>
        public T SwitchCamera<T>() where T : VirtualCameraController
        {
            return SwitchCamera(typeof(T)) as T;
        }

        public T GetCurrentCamera<T>() where T : VirtualCameraController
        {
            if (_currentCamera is T matched) return matched;

            Debug.LogError($"현재 활성 카메라({_currentCamera?.GetType().Name})가 요청하신 {typeof(T).Name}와 다릅니다.");
            return null;
        }

        private void SetActiveCamera(VirtualCameraController target)
        {
            if (_currentCamera == target) return;

            // 비활성화 (GameObject를 끄지 않고 Priority를 0으로)
            if (_currentCamera != null)
                _currentCamera.SetActive(false);

            // 활성화 (Priority를 10으로 올려서 렌즈를 가져옴)
            _currentCamera = target;
            _currentCamera.SetActive(true);
        }

        public void ResetCamera()
        {
            // OptionChanged(OptionManager.appliedGraphicOption);
        }

        /* 옵션 매니저 연동용 주석 처리
        public void OptionChanged(GraphicOptionValues value)
        {
            _currentCamera?.SetVerticalFOV(value.fileldOfView);
        }
        */
    }
}