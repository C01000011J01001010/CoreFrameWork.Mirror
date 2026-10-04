using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine.Helpers
{
    public class HashHelper
    {
        /// <summary>
        /// 문자열을 충돌 없는 64비트 ulong으로 변환
        /// </summary>
        public static ulong GetHash64(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            ulong hash = 14695981039346656037; // FNV-1a 64-bit offset basis
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 1099511628211; // FNV-1a 64-bit prime
            }
            return hash;
        }

        public static ulong StringToId(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            if (ulong.TryParse(key, out ulong numericId)) return numericId;
            return GetHash64(key);
        }
    }
}
