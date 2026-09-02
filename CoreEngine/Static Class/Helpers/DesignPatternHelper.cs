using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine.Helpers
{
    public static class DesignPatternHelper
    {
        /// <summary>
        /// 싱글톤 만들때 수백번 필요한지 생각할것
        /// </summary>
        public static bool TryMakeSingleton<T>(T target, ref T slot) where T : MonoBehaviour
        {
            if (target == null) return false;
            else if (CanSingleTon(target, ref slot))
            {
                slot = target;
                return true;
            }

            // 이미 싱글톤으로 된 다른 객체가 존재하는 경우
            LogHelper.LogWarningSingleTon<T>(target); // no-op
            return false;
        }

        private static bool CanSingleTon<T>(T target, ref T slot) where T : MonoBehaviour
            => slot == null || target == slot;
    }
}
