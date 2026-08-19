using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine
{
    public static class UtilitySystem
    {
        public static bool isUnityNull<T>(T obj)
        {
            // 순수 C# 차원에서의 진짜 Null 검사
            // 최적화를 위해 연산자 오버로딩이 없는 is 사용
            if (obj is null)
            {
                return true;
            }
            // 객체가 유니티 엔진의 객체(UnityEngine.Object)인 경우에만 Fake Null 검사
            if (obj is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }
            return false;
        }

        #region IsAppQuitting
        // 1. 앱 종료 상태를 저장할 전역 프로퍼티
        public static bool IsAppQuitting { get; private set; } = false;

        // 2. 게임 시작 시 유니티 엔진이 자동으로 이 메서드를 찾아 실행함
        // SubsystemRegistration 타이밍에 실행하여 에디터 플레이 모드 반복 시에도 완벽히 초기화됨
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitAppQuitState()
        {
            // 에디터에서 플레이 모드를 껐다 켤 때 static 변수가 남아있는 것을 방지
            IsAppQuitting = false;

            // 이벤트 중복 구독 방지 후 구독
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        // 3. 앱이 종료될 때 유니티가 호출해주는 콜백
        private static void OnApplicationQuitting()
        {
            IsAppQuitting = true;
        }
        #endregion
    }
}
