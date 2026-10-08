using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;

namespace CoreEditor.GameData
{
    [System.Serializable]
    public class TableMetaData
    {
        public string csvFileName;
        public string[] primaryKey;
    }

    public static class CsvImportPipeline
    {
        /// <summary> 리플렉션을 통해 제네릭 파이프라인을 호출하는 진입점 </summary>
        internal static void ProcessTable(Type recordType, ITableSetter tableSetter, TextAsset csvAsset)
        {
            MethodInfo method = typeof(CsvImportPipeline).GetMethod(nameof(ProcessTableGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            method.MakeGenericMethod(recordType).Invoke(null, new object[] { tableSetter, csvAsset });
        }

        private static void ProcessTableGeneric<TRecord>(ITableSetter tableSetter, TextAsset csvAsset)
            where TRecord : BaseRecord, IRecord, IEditorRecordSetup, new()
        {
            // 0. 메타데이터(.json) 로드
            string csvPath = AssetDatabase.GetAssetPath(csvAsset);
            string jsonPath = Path.ChangeExtension(csvPath, ".json");

            if (!File.Exists(jsonPath))
                throw new Exception($"[메타 누락] {jsonPath} 파일을 찾을 수 없습니다. 파이썬 툴에서 메타데이터를 추출해주세요.");

            string jsonText = File.ReadAllText(jsonPath);
            TableMetaData metaData = JsonUtility.FromJson<TableMetaData>(jsonText);

            if (metaData.primaryKey == null || metaData.primaryKey.Length == 0)
                throw new Exception($"[메타 오류] {csvAsset.name}.json 에 기본키(PK)가 설정되어 있지 않습니다.");

            // 1. 파싱 엔진을 통한 데이터 파싱
            var records = CsvRecordParser.Parse<TRecord>(csvAsset.text, metaData, out List<string> generatedRawKeys);

            // 2. 프리로드 커맨드 추출
            var bakedCommands = PreloadCommandExtractor.Extract<TRecord>(records);

            // 3. SO 에셋 저장 (Baking)
            tableSetter.Clear();
            tableSetter.Set(records);
            tableSetter.BakePreloadCommands(bakedCommands);
            EditorUtility.SetDirty((UnityEngine.Object)tableSetter);

            // 4. 상수 키 C# 스크립트 생성
            KeyClassGenerator.Generate(csvAsset, typeof(TRecord), generatedRawKeys);
        }
    }
}