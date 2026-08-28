using CoreEngine.EventBus;
using CoreEngine.Manager.Culling;
using System.Diagnostics;
using UnityEngine;

namespace CoreEngine
{
    // 장애물 및 모든 Culling 객체의 부모 액터
    public abstract class BaseCullingObjectActor : BaseActor, ICullingObject
    {
#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private float _deubgGizmoSize = 1.0f;
        [HideInInspector][SerializeField] protected bool _isVisualActive = true;
        [HideInInspector][SerializeField] protected bool _isPhysicsActive = true;
#endif

        [Header("Culling Options")]
        [Tooltip("Culling에 상관없이 항상 보여야하는 객체면 true로 설정")]
        [SerializeField] private bool _isVisualAlwaysOn = false;

        public bool IsVisualAlwaysOn => _isVisualAlwaysOn;
        public abstract CullingType cullingType { get; }

        protected Renderer[] _renderers;
        protected Collider[] _colliders;
        protected Rigidbody[] _rigidbodies;

        #region 디버깅 속성, 메서드
        // 🌟 기즈모 디버깅을 위한 상태 캐싱 (기본값 true)

        [Conditional("UNITY_EDITOR")]
        protected void SetFlagVisualActive(bool isVisualActive)
        {
#if UNITY_EDITOR
            _isVisualActive = isVisualActive;
#endif
        }

        [Conditional("UNITY_EDITOR")]
        protected void SetFlagPhysicsActive(bool isPhysicsActive)
        {
#if UNITY_EDITOR
            _isPhysicsActive = isPhysicsActive;
#endif
        }
        #endregion

        protected virtual void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            _rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        }

        protected virtual void Start()
        {
            EventBus<CullingObjectRegistrationEvent>.Publish(new CullingObjectRegistrationEvent(this, true));
        }

        protected virtual void OnDestroy()
        {
            EventBus<CullingObjectRegistrationEvent>.Publish(new CullingObjectRegistrationEvent(this, false));
        }

        #region 자식들이 구현할 Culling 메서드
        public abstract void SetVisualActive(bool isActive);
        public abstract void SetPhysicsActive(bool isActive);
        #endregion

#if UNITY_EDITOR
        // 🎨 Editor 전용 시각화 디버깅
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // R+1 범위 제한 (시각적으로 꺼져있으면 아예 그리지 않음!)
            // 정적 객체는 activeSelf=false라 애초에 이 함수가 안 불리고, 
            // 동적 객체는 여기서 완벽하게 필터링
            if (!_isVisualActive) return;

            DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            // 에디터 환경에서 개별 체크할 수 있도록
            DrawGizmos();
        }
        private void DrawGizmos()
        {
            // 1보다 크게 유지
            _deubgGizmoSize = Mathf.Max(1, _deubgGizmoSize);

            // 물리 상태에 따라 색상 결정 (On: 초록, Off: 빨강)
            Color gizmoColor = _isPhysicsActive ? Color.green : Color.red;

            UnityEditor.Handles.color = gizmoColor;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _deubgGizmoSize);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, _deubgGizmoSize);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.right, _deubgGizmoSize);
        }

#endif
    }
}