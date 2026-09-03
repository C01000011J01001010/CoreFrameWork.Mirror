using CoreEngine.EventBus;
using CoreEngine.Interface;
using Unity.Cinemachine;
using UnityEngine;
using CoreEngine.Helpers;
using CoreEngine.Manager.Input;
using CoreEngine.Manager;

namespace CoreEngine.CameraSystem
{
    public struct ToggleMouseLockEvent : IEvent
    {
        public bool IsMouseLock { get; }
        public ToggleMouseLockEvent(bool isMouseLock) => this.IsMouseLock = isMouseLock;
    }

    [RequireComponent(typeof(CinemachineThirdPersonFollow))]
    public class ThirdPersonCameraController : VirtualCameraController
    {
        [SerializeField] private CameraZoomHandler _zoomHandler = new();
        [SerializeField] private CameraRotationHandler _rotationHandler = new();

        private CinemachineThirdPersonFollow _thirdPersonFollow;

        private InterfaceReceiver<ILookInput> _lookReceiver = new();
        private InterfaceReceiver<IScollDeltaInput> _ScrollDeltaReceiver = new();

        private ILookInput _lookInput;
        private ILookInput LookInput
        {
            get
            {
                if (_lookInput != null) return _lookInput;
                _lookReceiver?.TryGet(out _lookInput);
                return _lookInput;
            }
        }

        private IScollDeltaInput _scollDeltaInput;
        private IScollDeltaInput ScollDeltaInput
        {
            get
            {
                if (_scollDeltaInput != null) return _scollDeltaInput;
                _ScrollDeltaReceiver?.TryGet(out _scollDeltaInput);
                return _scollDeltaInput;
            }
        }

        private bool isMouseLock = true;

        protected override void Awake()
        {
            base.Awake();
            _thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();

            // 줌 핸들러 초기화
            if (_thirdPersonFollow != null)
            {
                _zoomHandler.Initialize(_thirdPersonFollow.CameraDistance);
            }
        }

        public override void OnSetTarget(SetCameraTargetEvent evt)
        {
            base.OnSetTarget(evt);

            // 회전 핸들러 초기화 (짐벌의 초기 각도를 캐싱하여 짐벌락 및 튀는 현상 방지)
            if (TrackingTarget != null)
            {
                _rotationHandler.Initialize(TrackingTarget.eulerAngles);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // CoreFacade를 통해 안전하게 의존성 주입 (입력 가져오기)
            _lookReceiver.Bind();
            _ScrollDeltaReceiver.Bind();
            EventBus<ToggleMouseLockEvent>.Subscribe(OnToggleMouseLock);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<ToggleMouseLockEvent>.Unsubscribe(OnToggleMouseLock);
        }

        private void OnToggleMouseLock(ToggleMouseLockEvent evt)
        {
            isMouseLock = evt.IsMouseLock;
        }

        protected override Transform FindTrackingTarget() => null;

        /// <summary>
        /// CameraManager가 매 프레임(LateUpdate 등) 호출해주는 지휘 본부
        /// </summary>
        public override void CameraTick(float deltaTime)
        {
            if (LookInput == null || ScollDeltaInput == null) return;

            // 1. 입력 가져오기 (Model -> Controller)

            // 2. 줌(Zoom) Worker에게 역할 위임
            _zoomHandler.OnZoomInput(ScollDeltaInput.value);
            _zoomHandler.ProcessZoom(_thirdPersonFollow, deltaTime);

            // 3. 회전(Rotation) Worker에게 역할 위임 및 결과 적용
            if (isMouseLock && !SystemHelper.isUnityNull(TrackingTarget))
            {
                // 핸들러는 순수하게 '계산된 회전값'만 반환합니다. (Side-Effect 없음)
                Quaternion newRotation = _rotationHandler.ProcessRotation(LookInput.value, deltaTime);

                // 지휘관(Controller)이 직접 투명한 카메라 짐벌(TrackingTarget)에 회전값을 덮어씌웁니다.
                // 주의: 물리 타겟(항아리)이 아닌 카메라 전용 짐벌이어야 합니다!
                TrackingTarget.rotation = newRotation;
            }
        }
    }
}