using System;
using System.IO;
using System.Collections;
using UnityEngine;
using CoreEngine.Data; // PathManager 사용
using CoreEngine.Helpers; // JsonSerializer (및 Binary) 사용

namespace CoreEngine.Manager
{
    public class DataManager : BaseManager, IPriority
    {
        public int Priority => (int)ManagerPriority.Infrastructure;

        //public GraphicOptionValues SavedGraphicOption { get; private set; }

        public override IEnumerator Initialize()
        {
            LoadLocalOption();
            yield break;
        }

        // =======================================================================
        // [파일 IO 로직 - 무거운 지도 데이터용 (바이너리)]
        // =======================================================================
        public void SaveFileBinary(string directory, string fileName, params byte[] data)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string totalPath = Path.Combine(directory, fileName);

            File.WriteAllBytes(totalPath, data);
        }

        public byte[] LoadFileBinary(string directory, string fileName)
        {
            string totalPath = Path.Combine(directory, fileName);
            return File.Exists(totalPath) ? File.ReadAllBytes(totalPath) : null;
        }

        // =======================================================================
        // [파일 IO 로직 - 환경설정, 메타데이터용 (텍스트/JSON)]
        // =======================================================================
        public void SaveFileText(string directory, string fileName, string textData)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string totalPath = Path.Combine(directory, fileName);

            File.WriteAllText(totalPath, textData);
        }

        public string LoadFileText(string directory, string fileName)
        {
            string totalPath = Path.Combine(directory, fileName);
            return File.Exists(totalPath) ? File.ReadAllText(totalPath) : null;
        }

        // =======================================================================
        // [옵션 세이브/로드 (JSON 방식으로 개선)]
        // =======================================================================
        private void LoadLocalOption()
        {
            //try
            //{
            //    // 1. 파일에서 텍스트(JSON) 읽기
            //    string savedJson = LoadFileText(GamePaths.OptionPath, GamePaths.GraphicSettingsFileName);

            //    if (!string.IsNullOrEmpty(savedJson))
            //    {
            //        // 2. JSON 유틸리티를 사용해 객체로 복원
            //        SavedGraphicOption = savedJson.FromJson<GraphicOptionValues>();
            //    }
            //    else
            //    {
            //        SavedGraphicOption = GraphicOptionValues.defaultOption;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    SavedGraphicOption = GraphicOptionValues.defaultOption;
            //    Debug.LogWarning($"[DataManager] 옵션 로드 실패, 기본값 적용: {ex.Message}");
            //}
        }

#if UNITY_EDITOR
        public void TestLocalOptionSave()
        {
            //GraphicOptionValues options = GraphicOptionValues.testOption;

            //// 1. 객체를 JSON 문자열로 변환
            //string jsonText = options.ToJson();

            //// 2. 텍스트 파일로 저장
            //SaveFileText(GamePaths.OptionPath, GamePaths.GraphicSettingsFileName, jsonText);

            //Debug.Log("[DataManager] 테스트 옵션 (JSON) 저장 완료");
        }
#endif
    }
}