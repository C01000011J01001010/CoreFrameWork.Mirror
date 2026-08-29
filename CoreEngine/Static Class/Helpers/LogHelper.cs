using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CoreEngine.Helpers
{
    public static class LogHelper
    {
        // 1. MonoBehaviour 대신 object로 받아 순수 C# 클래스(Hub, Data 등)에서도 쓸 수 있게 범용성 확장!
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogFunctionCallStart(
            object caller,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            // 함수 내용도 #if로 감싸야 완벽하게 오버헤드 0이 됩니다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string fileName = System.IO.Path.GetFileName(sourceFilePath);
            string callerName = caller != null ? caller.GetType().Name : "Static/Unknown";

            // MonoBehaviour일 경우에만 게임오브젝트 이름을 뽑아오도록 안전하게 캐스팅
            string objName = caller is MonoBehaviour mono ? $"[{mono.gameObject.name}] " : "";

            UnityEngine.Debug.Log($"<color=cyan>{objName}</color>{callerName}." +
                      $"<b>{memberName}</b> 시작 " +
                      $"<color=grey>(File: {fileName}, Line: {sourceLineNumber})</color>");
#endif
        }

        #region Count 로그
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static Dictionary<string, int> _callCounts = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCounts()
        {
            _callCounts.Clear();
        }
#endif

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogFunctionCallCount(
            object caller,
            [CallerMemberName] string memberName = "")
        {
            // 🚨 주의: _callCounts가 #if 안에 있으므로, 여기도 무조건 #if로 감싸야 빌드 에러가 안 납니다!
#if UNITY_EDITOR || DEVELOPMENT_BUILD 
            string callerName = caller != null ? caller.GetType().Name : "Static/Unknown";
            string key = $"{callerName}.{memberName}";

            if (!_callCounts.ContainsKey(key))
            {
                _callCounts.Add(key, 0);
            }

            _callCounts[key]++;

            UnityEngine.Debug.Log($"<color=orange>[Count]</color> <b>{key}</b> 실행 횟수 : <color=red>{_callCounts[key]}</color>");
#endif
        }
        #endregion

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarningSingleTon<T>(T target) where T : MonoBehaviour
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogWarning($"<color=yellow>[BaseDirector]</color> " +
                    $"{typeof(T).Name} 중복 객체 발견!" +
                    $"(위치: {target.gameObject.name})");
#endif
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarningDontInstance<T>()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogWarning($"<color=red>[BaseDirector]</color> " +
                $"씬에 {typeof(T).Name} 객체가 없습니다! " +
                $"하이라키를 확인하세요.");
#endif
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, string color = LogColor.Default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (color != LogColor.Default) message = $"<color={color}>{message}</color>";
            UnityEngine.Debug.Log(message);
#endif
        }
    }

    // 전역에서 아주 예쁘고 직관적으로 쓸 수 있는 최고의 구조입니다!
    public static class LogColor
    {
        // Color를 쓰지 않는 경우
        public const string Default = "";

        // Color를 쓰는 경우
        public const string Red = "red";
        public const string Green = "green";
        public const string Blue = "blue";
        public const string Yellow = "yellow";
        public const string Cyan = "cyan";
        public const string Magenta = "magenta";
        public const string Orange = "orange";
    }
}