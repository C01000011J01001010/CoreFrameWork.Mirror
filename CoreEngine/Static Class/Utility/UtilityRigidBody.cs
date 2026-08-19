using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine
{
    public static class UtilityRigidBody
    {
        /// <summary>
        /// Rigidbody를 주어진 방향으로 부드럽고 안전하게 회전시킵니다. (짐벌락 방지 및 물리 충돌 방지)
        /// </summary>
        /// <param name="rb">회전시킬 주체 (Rigidbody)</param>
        /// <param name="direction">바라볼 방향 벡터</param>
        /// <param name="rotationSpeed">회전 속도</param>
        public static void SmoothLookAt(Rigidbody rb, Vector3 direction, float rotationSpeed, float fixedDelta)
        {
            // 1. 예외 처리: Rigidbody가 없거나, 입력 방향이 거의 0에 가까우면 무시
            if (rb == null || direction.sqrMagnitude < 0.01f)
                return;

            // 2. 평면 고정: 위아래(Y축)를 보며 고꾸라지는 현상 방지
            Vector3 lookDirection = new Vector3(direction.x, 0f, direction.z).normalized;

            // 3. 목표 쿼터니언 회전값 계산
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // 4. 현재 회전에서 목표 회전으로 부드럽게 Slerp (구면 선형 보간)
            // 물리 연산이므로 Time.deltaTime이 아닌 Time.fixedDeltaTime 사용!
            Quaternion smoothRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                fixedDelta * rotationSpeed
            );

            // 5. Transform 대신 물리 파이프라인을 통한 안전한 회전 적용
            rb.MoveRotation(smoothRotation);
        }
    }
}
