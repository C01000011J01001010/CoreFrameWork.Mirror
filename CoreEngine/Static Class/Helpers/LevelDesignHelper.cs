using UnityEngine;

namespace CoreEngine.Helpers
{
    public static class LevelDesignHelper
    {
        /// <summary>
        /// 3D 월드 좌표를 맵 이미지 상의 0.0 ~ 1.0 비율(Normalized) 좌표로 변환합니다.
        /// </summary>
        public static Vector2 GetNormalizedMapPosition(Vector3 playerWorldPos, Vector2 minBounds, Vector2 maxBounds)
        {
            float normalizedX = Mathf.InverseLerp(minBounds.x, maxBounds.x, playerWorldPos.x);
            float normalizedY = Mathf.InverseLerp(minBounds.y, maxBounds.y, playerWorldPos.z); // 3D의 Z축이 2D의 Y축이 됨!

            return new Vector2(normalizedX, normalizedY);
        }
    }
}