using System;
using UnityEngine;

namespace CoreEngine.CameraSystem
{
    [Serializable]
    public class CameraRotationHandler
    {
        [Header("Rotation Speed")]
        [Tooltip("좌우 회전 속도 (Yaw)")]
        public float horizontalSpeed = 150f;

        [Tooltip("상하 회전 속도 (Pitch)")]
        public float verticalSpeed = 100f;

        [Header("Rotation Limits")]
        [Tooltip("카메라가 위로 넘어가지 않도록 하는 최소 각도")]
        public float minPitch = -30f;

        [Tooltip("카메라가 아래로 넘어가지 않도록 하는 최대 각도")]
        public float maxPitch = 60f;

        [Header("Settings")]
        [Tooltip("Y축 반전 여부")]
        public bool invertY = false;

        // 짐벌락(Gimbal Lock) 방지를 위해 현재 각도를 내부에 캐싱합니다.
        private float _yaw = 0f;
        private float _pitch = 0f;

        /// <summary>
        /// 초기 기준이 될 회전 각도(Euler)를 입력받아 세팅합니다.
        /// 물리 타겟이 아닌, 카메라 짐벌(Pivot)의 초기 각도를 넣는 것이 좋습니다.
        /// </summary>
        public void Initialize(Vector3 initialEulerAngles)
        {
            _yaw = initialEulerAngles.y;
            _pitch = initialEulerAngles.x;

            // 유니티 0~360도 체계를 -180~180도로 정규화하여 Clamp 오류 방지
            if (_pitch > 180f) _pitch -= 360f;
        }

        /// <summary>
        /// 마우스 입력(lookInput)을 받아 계산된 '회전값(Quaternion)'만 순수하게 반환합니다.
        /// 타겟 오브젝트에 직접적인 영향을 주지 않습니다(No Side-Effects).
        /// </summary>
        public Quaternion ProcessRotation(Vector2 lookInput, float deltaTime)
        {
            // 입력이 있을 때만 각도 누적 연산 진행
            if (lookInput != Vector2.zero)
            {
                _yaw += lookInput.x * horizontalSpeed * deltaTime;

                float pitchDelta = lookInput.y * verticalSpeed * deltaTime;
                _pitch += invertY ? pitchDelta : -pitchDelta;

                // 수직 각도 제한 (Clamp) - 카메라가 뒤집히는 것 방지
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            // 계산된 최종 회전값만 반환
            return Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}