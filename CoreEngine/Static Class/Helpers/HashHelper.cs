using System.IO.Hashing;
using System.Text;

namespace CoreEngine.Helpers
{
    public static class HashHelper
    {
        /// <summary>
        /// 문자열을 숫자로 변환하거나, 불가능할 경우 XXH64 해시값(ulong)을 반환합니다.
        /// </summary>
        public static ulong StringToId(string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0; // 0은 Invalid/Empty ID로 예약

            // 이미 숫자로 된 문자열이면 그대로 ulong 변환
            if (ulong.TryParse(key, out ulong numericId))
            {
                return numericId;
            }

            // 일반 문자열이면 XXH64 알고리즘으로 64비트 해시 생성
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            return XxHash64.HashToUInt64(bytes);
        }
    }
}