using CoreEngine;
using CoreEngine.EventBus;
using CoreEngine.Manager;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 개별씬에서 GlobalScene에 존재하는 CameraManager에 자신을 등록함
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public abstract class VirtualCameraController : BaseActor
{
    [SerializeField] protected CinemachineCamera cinemachineCamera;

    protected Transform TrackingTarget { get; private set; }

    protected virtual void Awake()
    {
        EventBus<SetCameraTargetEvent>.Subscribe(OnSetTarget);
    }

    private void OnDestroy()
    {
        EventBus<SetCameraTargetEvent>.Unsubscribe(OnSetTarget);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // CameraManager에게 나(VirtualCamera)를 등록
        EventBus<RegisterVirtualCameraEvent>.Publish(new RegisterVirtualCameraEvent(this, true));
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // CameraManager에서 나를 해제
        EventBus<RegisterVirtualCameraEvent>.Publish(new RegisterVirtualCameraEvent(this, false));
    }

    /// <summary>
    /// 카메라 활성/비활성 (Priority 제어로 Blending 활성화 및 생명주기 충돌 방지)
    /// </summary>
    public virtual void SetActive(bool active)
    {
        if (cinemachineCamera != null)
        {
            // 활성화되면 10, 비활성화되면 0으로 설정하여 시네머신 뇌가 부드럽게 전환하게 함
            cinemachineCamera.Priority = active ? 10 : 0;
        }
    }

    public void OnSetTarget(SetCameraTargetEvent evt)
    {
        // 이벤트에 특정 타겟 카메라가 지정되어 있는데, 내 타입이 아니라면 무시! (전체 카메라가 돌아가는 것 방지)
        if (evt.targetCameraType != null && evt.targetCameraType != this.GetType())
            return;

        SetTrackingTarget(evt.target);
    }

    protected void SetTrackingTarget(Transform trackingTarget = null, Transform lookAtTarget = null)
    {
        if (trackingTarget == null && cinemachineCamera.Target.TrackingTarget) return;

        if (trackingTarget == null) trackingTarget = FindTrackingTarget();
        if (lookAtTarget == null) lookAtTarget = FindLookAtTarget();

        if (trackingTarget == null) return;

        CameraTarget newTarget = new CameraTarget();
        newTarget.TrackingTarget = trackingTarget;

        if (lookAtTarget != null)
        {
            newTarget.CustomLookAtTarget = lookAtTarget;
            newTarget.LookAtTarget = lookAtTarget;
        }

        cinemachineCamera.Target = newTarget;
        TrackingTarget = cinemachineCamera.Target.TrackingTarget;
    }

    protected abstract Transform FindTrackingTarget();
    protected virtual Transform FindLookAtTarget() => null;

    public virtual void SetVerticalFOV(float value)
    {
        cinemachineCamera.Lens.FieldOfView = value;
    }

    /// <summary>
    /// 매니저의 LateTick에서 호출해주는 개별 카메라의 커스텀 업데이트 (줌, 마우스 회전 등)
    /// </summary>
    public virtual void CameraTick(float dt) { }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }
        if (cinemachineCamera != null)
        {
            // 에디터로 인한 트래킹 타겟 변경시 실시간 업데이트
            TrackingTarget = cinemachineCamera.Target.TrackingTarget;
        }
    }
#endif
}