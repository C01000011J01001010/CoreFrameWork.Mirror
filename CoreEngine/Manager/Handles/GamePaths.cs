using System.IO;
using UnityEngine;

namespace CoreEngine.Data
{
    /// <summary>
    /// 게임 내 모든 저장/로드 경로를 전역적으로 제공하는 순수 유틸리티
    /// </summary>
    public static class GamePaths
    {
        public static readonly string MainPath;
        public static readonly string SavePath;
        public static readonly string OptionPath;

        // 파일명 정의
        public const string GraphicSettingsFileName = "GraphicSettings.save";

        // 정적 생성자 (앱 시작 시 최초 1회 자동 초기화)
        static GamePaths()
        {
            MainPath = Path.Combine(Application.persistentDataPath, "Datas");
            SavePath = Path.Combine(MainPath, "Saves");
            OptionPath = Path.Combine(MainPath, "Options");

#if UNITY_EDITOR
            Debug.Log($"[PathManager] 저장 경로 세팅 완료: {MainPath}");
#endif
        }
    }
}