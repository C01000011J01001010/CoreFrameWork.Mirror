using System;
using UnityEngine;
using Unity.Cinemachine;
namespace CoreEngine.CameraSystem
{
    [Serializable]
    public class CameraZoomHandler
    {
        [Header("Zoom Settings")]
        [Tooltip("카메라 줌인/아웃 1회 적용 비율")]
        [Range(0.05f, 0.5f)] public float zoomRate = 0.2f;

        [Tooltip("카메라 줌 부드러운 보간 속도")]
        [Range(5f, 15f)] public float zoomSpeed = 10f;

        [Header("Zoom Limits")]
        [Tooltip("최대 줌 인 (가장 가까움)")]
        public float zoomInLimit = 2f;

        [Tooltip("최대 줌 아웃 (가장 멂)")]
        public float zoomOutLimit = 10f;

        private float _targetZoomDistance;

        /// <summary>
        /// 초기 카메라 거리를 기준으로 타겟 거리를 세팅합니다.
        /// </summary>
        public void Initialize(float currentDistance)
        {
            _targetZoomDistance = Mathf.Clamp(currentDistance, zoomInLimit, zoomOutLimit);
        }

        /// <summary>
        /// 마우스 휠 입력(delta)을 받아 목표 거리를 계산합니다.
        /// </summary>
        public void OnZoomInput(float delta)
        {
            if (Mathf.Abs(delta) <= 0.01f) return;

            _targetZoomDistance -= delta * zoomRate;
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance, zoomInLimit, zoomOutLimit);
        }

        /// <summary>
        /// 계산된 목표 거리로 실제 시네머신 카메라의 렌즈 거리를 부드럽게(Lerp) 조절합니다.
        /// </summary>
        public void ProcessZoom(CinemachineThirdPersonFollow followComponent, float deltaTime)
        {
            if (followComponent == null) return;

            float currentDist = followComponent.CameraDistance;

            // 목표 거리와 현재 거리의 차이가 있을 때만 Lerp 연산 수행 (최적화)
            if (Mathf.Abs(currentDist - _targetZoomDistance) > 0.01f)
            {
                followComponent.CameraDistance = Mathf.Lerp(currentDist, _targetZoomDistance, deltaTime * zoomSpeed);
            }
        }
    }
}