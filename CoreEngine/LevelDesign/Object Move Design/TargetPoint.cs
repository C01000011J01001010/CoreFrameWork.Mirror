using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CoreEngine.LevelDesign
{
    public enum MovementMode { Linear, EaseInOut, EaseOut, CustomCurve }
    public enum LoopMode { None, Restart, PingPong, Circular }
    public enum UpdateMode { Transform, Rigidbody }
    public enum CollisionDetectionOption { Auto, ForceDiscrete, ForceContinuousSpeculative }

    [Serializable]
    public class TargetPoint
    {
        public Vector3 position;
        public Vector3 rotation;
        public float duration = 1f;
        public float waitTime = 0f;
        public MovementMode moveMode = MovementMode.EaseInOut;
        public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public UnityEvent onPointReached;
    }

    public class CoordinateTransformer
    {
        // 1. 토글 변경 시 좌표계 스왑 (절대 <-> 상대)
        public void ConvertCoordinates(TargetPoint rootPoint, List<TargetPoint> points, Transform refTransform, bool toRelative)
        {
            if (rootPoint != null) ConvertSinglePoint(rootPoint, refTransform, toRelative);
            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                    ConvertSinglePoint(points[i], refTransform, toRelative);
            }
        }

        private void ConvertSinglePoint(TargetPoint pt, Transform refTransform, bool toRelative)
        {
            if (pt == null) return;
            if (toRelative)
            {
                pt.position = refTransform != null ? refTransform.InverseTransformPoint(pt.position) : pt.position;
                pt.rotation = refTransform != null ? (Quaternion.Inverse(refTransform.rotation) * Quaternion.Euler(pt.rotation)).eulerAngles : pt.rotation;
            }
            else
            {
                pt.position = refTransform != null ? refTransform.TransformPoint(pt.position) : pt.position;
                pt.rotation = refTransform != null ? (refTransform.rotation * Quaternion.Euler(pt.rotation)).eulerAngles : pt.rotation;
            }
        }

        // 2. 부모 객체가 교체되었을 때 (Reparenting 방어)
        public void HandleReparenting(TargetPoint pt, Matrix4x4 oldParentMatrix, Transform newParent, bool useRelative)
        {
            if (pt == null || !useRelative) return;

            // 구 부모 매트릭스를 통해 월드 좌표 복원
            Vector3 worldPos = oldParentMatrix.MultiplyPoint3x4(pt.position);
            Quaternion worldRot = oldParentMatrix.rotation * Quaternion.Euler(pt.rotation);

            // 새 부모 기준으로 다시 로컬 좌표 덮어쓰기
            if (newParent != null)
            {
                pt.position = newParent.InverseTransformPoint(worldPos);
                pt.rotation = (Quaternion.Inverse(newParent.rotation) * worldRot).eulerAngles;
            }
            else
            {
                pt.position = worldPos;
                pt.rotation = worldRot.eulerAngles;
            }
        }

        // 3. 절대좌표 모드일 때 부모가 이동한 변화량(Delta)을 강제로 점들에게 먹여 끌고 옴
        public void ApplyDeltaTransform(TargetPoint pt, Vector3 deltaPos, Quaternion deltaRot, Vector3 pivotPos)
        {
            if (pt == null) return;
            Vector3 offset = pt.position - pivotPos;
            pt.position = pivotPos + (deltaRot * offset) + deltaPos;
            pt.rotation = (deltaRot * Quaternion.Euler(pt.rotation)).eulerAngles;
        }

        public Vector3 GetWorldPosition(TargetPoint pt, Transform refTransform, bool useRelative)
        {
            if (pt == null) return Vector3.zero;
            if (useRelative && refTransform != null)
            {
                return refTransform.TransformPoint(pt.position);
            }
            return pt.position;
        }

        public Quaternion GetWorldRotation(TargetPoint pt, Transform refTransform, bool useRelative)
        {
            if (pt == null) return Quaternion.identity;
            if (useRelative && refTransform != null)
            {
                return refTransform.rotation * Quaternion.Euler(pt.rotation);
            }
            return Quaternion.Euler(pt.rotation);
        }

        public void SetWorldPositionAndRotation(TargetPoint pt, Vector3 wPos, Quaternion wRot, Transform refTransform, bool useRelative)
        {
            if (pt == null) return;
            if (useRelative && refTransform != null)
            {
                pt.position = refTransform.InverseTransformPoint(wPos);
                pt.rotation = (Quaternion.Inverse(refTransform.rotation) * wRot).eulerAngles;
            }
            else
            {
                pt.position = wPos;
                pt.rotation = wRot.eulerAngles;
            }
        }
    }

    public class PathPhysicsEvaluator
    {
        private readonly float _fastSpeedThreshold;
        private readonly float _fastRotationThreshold;

        public PathPhysicsEvaluator(float fastSpeedThreshold, float fastRotationThreshold)
        {
            _fastSpeedThreshold = fastSpeedThreshold;
            _fastRotationThreshold = fastRotationThreshold;
        }

        public CollisionDetectionMode EvaluateCollisionMode(CollisionDetectionOption option, PathMovementController controller)
        {
            if (option == CollisionDetectionOption.ForceDiscrete) return CollisionDetectionMode.Discrete;
            if (option == CollisionDetectionOption.ForceContinuousSpeculative) return CollisionDetectionMode.ContinuousSpeculative;

            return CheckIsFastPathExists(controller) ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Discrete;
        }

        private bool CheckIsFastPathExists(PathMovementController controller)
        {
            var points = controller.points;
            var loopMode = controller.loopMode;

            if (points == null || points.Count == 0) return false;

            TargetPoint rootPoint = controller.rootPoint;
            int maxIdx = loopMode == LoopMode.Restart ? points.Count : points.Count - 1;
            TargetPoint GetPoint(int idx) => (loopMode == LoopMode.Restart && idx == points.Count) ? rootPoint : points[idx];

            Vector3 rootPos = controller.GetWorldPosition(rootPoint);
            Quaternion rootRot = controller.GetWorldRotation(rootPoint);
            Vector3 firstPos = controller.GetWorldPosition(points[0]);
            Quaternion firstRot = controller.GetWorldRotation(points[0]);

            if (IsSegmentFast(rootPos, firstPos, rootRot, firstRot, points[0].duration)) return true;

            for (int i = 0; i < maxIdx; i++)
            {
                TargetPoint p1 = GetPoint(i);
                TargetPoint p2 = GetPoint(i + 1);

                Vector3 wP1 = controller.GetWorldPosition(p1);
                Vector3 wP2 = controller.GetWorldPosition(p2);
                Quaternion wR1 = controller.GetWorldRotation(p1);
                Quaternion wR2 = controller.GetWorldRotation(p2);

                if (IsSegmentFast(wP1, wP2, wR1, wR2, p2.duration)) return true;
            }

            if (loopMode == LoopMode.Circular && points.Count > 1)
            {
                TargetPoint lastPoint = points[points.Count - 1];
                Vector3 lastPos = controller.GetWorldPosition(lastPoint);
                Quaternion lastRot = controller.GetWorldRotation(lastPoint);

                if (IsSegmentFast(lastPos, firstPos, lastRot, firstRot, points[0].duration)) return true;
            }

            return false;
        }

        private bool IsSegmentFast(Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot, float duration)
        {
            if (duration <= 0.0001f) return true;
            float linearSpeed = Vector3.Distance(startPos, endPos) / duration;
            float angularSpeed = Quaternion.Angle(startRot, endRot) / duration;
            return (linearSpeed >= _fastSpeedThreshold) || (angularSpeed >= _fastRotationThreshold);
        }
    }
}