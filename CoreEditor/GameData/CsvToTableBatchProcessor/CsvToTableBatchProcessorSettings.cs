using CoreEngine.DesignPattern.Singleton;
using UnityEngine;

namespace CoreEditor.GameData
{
    public class CsvToTableBatchProcessorSettings : BaseToolSettings<CsvToTableBatchProcessorSettings>
    {
        [Tooltip("폴더명이나 파일명이 이 문자열로 시작하면 스캔에서 제외됩니다.")]
        public string IgnorePrefix = "Disabled";
    }
}
