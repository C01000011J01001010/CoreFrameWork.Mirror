using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.LevelDesign
{
    public class PathMovementController : MonoBehaviour //, ITickable
    {
        private enum PathState { Moving, Waiting, Finished }

        [Header("System Settings")]
        public UpdateMode updateMode = UpdateMode.Rigidbody;
        public LoopMode loopMode = LoopMode.Restart;

        [Header("Coordinate Settings")]
        [Tooltip("상대좌표 사용 여부. 토글 변경 시 월드 위치는 유지되며 수치만 보정됩니다.")]
        public bool useRelativeCoordinates = false;
        [HideInInspector, SerializeField] private bool _lastRelativeState = false;

        [Tooltip("경로를 대상 객체(자기 자신) 기준으로 고정합니다. 체크 시 오브젝트 이동 시 전체 경로가 따라 움직입니다.")]
        public bool isPathRelativeToObject = false;

        [Tooltip("에디터 기즈모 핸들을 조작할 때 대상의 회전축(Local)을 따를지, 월드축(Global)을 따를지 결정합니다.")]
        public bool useLocalGizmo = true;

        [Header("Physics Detection Automation")]
        public CollisionDetectionOption collisionOption = CollisionDetectionOption.Auto;
        [SerializeField] private float _fastSpeedThreshold = 12f;
        [SerializeField] private float _fastRotationThreshold = 180f;

        [Header("Origin Root Point")]
        public TargetPoint rootPoint = new TargetPoint();

        [Header("Path Points")]
        public List<TargetPoint> points = new List<TargetPoint>();

        // 🌟 스냅샷 추적 변수들
        [HideInInspector, SerializeField] private Transform _lastGhostParentRef;
        [HideInInspector, SerializeField] private Vector3 _lastParentPos;
        [HideInInspector, SerializeField] private Quaternion _lastParentRot = Quaternion.identity;
        [HideInInspector, SerializeField] private Vector3 _lastParentScale = Vector3.one;
        [HideInInspector, SerializeField] private bool _isParentTracked = false;

        // --- Has-A Components ---
        private readonly CoordinateTransformer _coordTransformer = new CoordinateTransformer();
        private PathPhysicsEvaluator _physicsEvaluator;
        private Rigidbody _rb;

        // --- State Machine Variables ---
        private PathState _currentState = PathState.Finished;
        private int _currentIndex = 0;
        private int _pingPongDirection = 1;
        private float _timer = 0f;

        private Vector3 _startPos;
        private Quaternion _startRot;
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        private Transform _runtimeReferenceParent;

        private void OnEnable()
        {
            if (!_isParentTracked)
            {
                _lastGhostParentRef = isPathRelativeToObject ? transform : transform.parent;
                CacheCurrentParentTransform(_lastGhostParentRef);
                _isParentTracked = true;
            }
        }

        private void Awake()
        {
            _runtimeReferenceParent = isPathRelativeToObject ? transform : transform.parent;

            SyncRootPointWithTransform();

            if (updateMode == UpdateMode.Rigidbody) InitRigidbodyPhysics();
        }

        private void Start()
        {
            if (points != null && points.Count > 0) SetupNextMove(0);
            else if (loopMode == LoopMode.Restart) SetupNextMove(0);
        }

        private void InitRigidbodyPhysics()
        {
            _rb = gameObject.GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();

            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _physicsEvaluator = new PathPhysicsEvaluator(_fastSpeedThreshold, _fastRotationThreshold);
            _rb.collisionDetectionMode = _physicsEvaluator.EvaluateCollisionMode(collisionOption, this);
        }

        public void Tick(float deltaTime)
        {
            if (_currentState == PathState.Finished) return;

            if (_currentState == PathState.Waiting)
            {
                _timer -= deltaTime;
                if (_timer <= 0f) DetermineNextIndex();
                return;
            }

            if (_currentState == PathState.Moving) ProcessMovement(deltaTime);
        }

        private void Update() { if (updateMode == UpdateMode.Transform) Tick(Time.deltaTime); }
        private void FixedUpdate() { if (updateMode == UpdateMode.Rigidbody) Tick(Time.fixedDeltaTime); }

        private void ProcessMovement(float dt)
        {
            TargetPoint currentPoint = GetTargetPoint(_currentIndex);
            if (currentPoint == null) return;

            _timer += dt;
            float normalizedTime = Mathf.Clamp01(_timer / currentPoint.duration);
            float curveValue = GetCurveValue(normalizedTime, currentPoint.moveMode, currentPoint.customCurve);

            Vector3 nextPos = Vector3.Lerp(_startPos, _targetPos, curveValue);
            Quaternion nextRot = Quaternion.Slerp(_startRot, _targetRot, curveValue);

            if (updateMode == UpdateMode.Rigidbody && _rb != null)
            {
                _rb.MovePosition(nextPos);
                _rb.MoveRotation(nextRot);
            }
            else
            {
                transform.position = nextPos;
                transform.rotation = nextRot;
            }

            if (normalizedTime >= 1f)
            {
                currentPoint.onPointReached?.Invoke();
                if (currentPoint.waitTime > 0f)
                {
                    _currentState = PathState.Waiting;
                    _timer = currentPoint.waitTime;
                }
                else
                {
                    DetermineNextIndex();
                }
            }
        }

        private void SetupNextMove(int index)
        {
            TargetPoint target = GetTargetPoint(index);
            if (target == null) return;

            _startPos = updateMode == UpdateMode.Rigidbody ? _rb.position : transform.position;
            _startRot = updateMode == UpdateMode.Rigidbody ? _rb.rotation : transform.rotation;

            _targetPos = GetWorldPosition(target);
            _targetRot = GetWorldRotation(target);

            _timer = 0f;
            _currentState = PathState.Moving;
        }

        private void DetermineNextIndex()
        {
            if (loopMode == LoopMode.PingPong)
            {
                if (_currentIndex == points.Count - 1) _pingPongDirection = -1;
                else if (_currentIndex == 0 && _pingPongDirection == -1) _pingPongDirection = 1;

                _currentIndex += _pingPongDirection;
                SetupNextMove(_currentIndex);
            }
            else
            {
                _currentIndex++;
                int maxIdx = loopMode == LoopMode.Restart ? points.Count : points.Count - 1;

                if (_currentIndex > maxIdx)
                {
                    if (loopMode == LoopMode.None)
                    {
                        _currentState = PathState.Finished;
                        return;
                    }

                    if (loopMode == LoopMode.Restart || loopMode == LoopMode.Circular)
                    {
                        _currentIndex = 0;
                        SetupNextMove(_currentIndex);
                    }
                }
                else
                {
                    SetupNextMove(_currentIndex);
                }
            }
        }

        private TargetPoint GetTargetPoint(int index)
        {
            if (index >= 0 && index < points.Count) return points[index];
            if (loopMode == LoopMode.Restart && index == points.Count) return rootPoint;
            return null;
        }

        private float GetCurveValue(float t, MovementMode mode, AnimationCurve curve)
        {
            switch (mode)
            {
                case MovementMode.EaseInOut: return Mathf.SmoothStep(0f, 1f, t);
                case MovementMode.EaseOut: return 1f - Mathf.Pow(1f - t, 2f);
                case MovementMode.CustomCurve: return curve != null ? curve.Evaluate(t) : t;
                case MovementMode.Linear:
                default: return t;
            }
        }

        #region Validation & Ghost Tracking Logic
#if UNITY_EDITOR
        private void Reset()
        {
            _lastGhostParentRef = isPathRelativeToObject ? transform : transform.parent;
            CacheCurrentParentTransform(_lastGhostParentRef);
            _isParentTracked = true;

            SyncRootPointWithTransform();
            if (points == null) points = new List<TargetPoint>();
            if (points.Count == 0)
            {
                TargetPoint defaultPt = new TargetPoint { duration = 1f };
                SetWorldPositionAndRotation(defaultPt, transform.position + transform.forward * 3f, transform.rotation);
                points.Add(defaultPt);
            }
        }

        private void OnValidate()
        {
            if (!_isParentTracked)
            {
                _lastGhostParentRef = isPathRelativeToObject ? transform : transform.parent;
                CacheCurrentParentTransform(_lastGhostParentRef);
                _isParentTracked = true;
            }

            // 토글 스왑 시 좌표계 변환 완벽 적용 (인수 검증 완료)
            if (_lastRelativeState != useRelativeCoordinates)
            {
                _coordTransformer.ConvertCoordinates(rootPoint, points, _lastGhostParentRef, useRelativeCoordinates);
                _lastRelativeState = useRelativeCoordinates;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            TrackTransformChanges();

            if (rootPoint != null)
            {
                if (rootPoint.duration <= 0f) rootPoint.duration = 0.1f;
                if (rootPoint.waitTime < 0f) rootPoint.waitTime = 0f;
            }

            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].duration <= 0f) points[i].duration = 0.1f;
                    if (points[i].waitTime < 0f) points[i].waitTime = 0f;
                }
            }
        }

        // 에디터 씬에서 움직일 때 실시간 추적을 위한 기즈모 업데이트
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) TrackTransformChanges();
        }
#endif

        // 🌟 통합된 위치 추적 엔진 (부모 변경 및 절대좌표 델타 이동 방어)
        public void TrackTransformChanges()
        {
            Transform currentGhostParent = isPathRelativeToObject ? transform : transform.parent;

            // CASE 1: 부모가 완전히 다른 객체로 교체된 경우 (Reparenting 방어)
            if (_lastGhostParentRef != currentGhostParent)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Reparent Path Points");
#endif
                Matrix4x4 oldParentMatrix = Matrix4x4.TRS(_lastParentPos, _lastParentRot, _lastParentScale);

                _coordTransformer.HandleReparenting(rootPoint, oldParentMatrix, currentGhostParent, useRelativeCoordinates);
                if (points != null)
                {
                    foreach (var pt in points)
                        _coordTransformer.HandleReparenting(pt, oldParentMatrix, currentGhostParent, useRelativeCoordinates);
                }

                _lastGhostParentRef = currentGhostParent;
                CacheCurrentParentTransform(currentGhostParent);
                return;
            }

            // CASE 2: 부모가 씬에서 이동/회전한 경우 (Absolute 모드일 때 버려지는 현상 완벽 추적)
            if (currentGhostParent != null && !useRelativeCoordinates)
            {
                if (_lastParentPos != currentGhostParent.position || _lastParentRot != currentGhostParent.rotation)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Move Absolute Path Points");
#endif
                    Vector3 deltaPos = currentGhostParent.position - _lastParentPos;
                    Quaternion deltaRot = currentGhostParent.rotation * Quaternion.Inverse(_lastParentRot);

                    _coordTransformer.ApplyDeltaTransform(rootPoint, deltaPos, deltaRot, _lastParentPos);
                    if (points != null)
                    {
                        foreach (var pt in points)
                            _coordTransformer.ApplyDeltaTransform(pt, deltaPos, deltaRot, _lastParentPos);
                    }
                }
            }

            CacheCurrentParentTransform(currentGhostParent);
            SyncRootPointWithTransform();
        }

        public void CacheCurrentParentTransform(Transform parentObj)
        {
            if (parentObj != null)
            {
                _lastParentPos = parentObj.position;
                _lastParentRot = parentObj.rotation;
                _lastParentScale = parentObj.lossyScale;
            }
            else
            {
                _lastParentPos = Vector3.zero;
                _lastParentRot = Quaternion.identity;
                _lastParentScale = Vector3.one;
            }
        }

        public void SyncRootPointWithTransform()
        {
            if (rootPoint == null) rootPoint = new TargetPoint();
            // 안전하게 내부 변환기를 거쳐서 월드/로컬 값을 100% 본체 위치에 맞춤
            SetWorldPositionAndRotation(rootPoint, transform.position, transform.rotation);
        }

        // --- 완벽히 일치된 래퍼(Wrapper) 헬퍼 메서드들 ---
        public Vector3 GetWorldPosition(TargetPoint pt) =>
            _coordTransformer.GetWorldPosition(pt, Application.isPlaying ? _runtimeReferenceParent : _lastGhostParentRef, useRelativeCoordinates);

        public Quaternion GetWorldRotation(TargetPoint pt) =>
            _coordTransformer.GetWorldRotation(pt, Application.isPlaying ? _runtimeReferenceParent : _lastGhostParentRef, useRelativeCoordinates);

        public void SetWorldPositionAndRotation(TargetPoint pt, Vector3 wPos, Quaternion wRot)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Modify Target Point");
#endif
            _coordTransformer.SetWorldPositionAndRotation(pt, wPos, wRot, Application.isPlaying ? _runtimeReferenceParent : _lastGhostParentRef, useRelativeCoordinates);
        }
        #endregion
    }
}