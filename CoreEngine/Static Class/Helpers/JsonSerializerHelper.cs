using System;
using UnityEngine;
using Newtonsoft.Json; // 패키지 매니저에서 설치한 Newtonsoft.Json 사용

namespace CoreEngine.Helpers
{
    /// <summary>
    /// 객체 <-> JSON 문자열 변환을 전담하는 직렬화 유틸리티
    /// </summary>
    public static class JsonSerializerHelper
    {
        // 1. 객체 -> JSON 문자열 변환
        public static string ToJson<T>(this T obj)
        {
            // Formatting.Indented를 사용하면 메모장으로 열었을 때 줄바꿈이 예쁘게 적용됩니다.
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        // 2. JSON 문자열 -> 객체 복원
        public static T FromJson<T>(this string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json)) return default;
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializer] 파싱 실패: {ex.Message}");
                return default;
            }
        }
    }
}