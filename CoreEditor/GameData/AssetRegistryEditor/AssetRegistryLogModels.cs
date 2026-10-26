using System;
using System.Collections.Generic;

namespace CoreEditor.GameData
{
    public static class AssetRegistryLogConstants
    {
        public const string MetaFileName = ".regimeta";
        public const string LogFileName = ".regilog";
    }

    /// <summary>
    /// 루트 폴더의 .regimeta JSON 직렬화 모델
    /// </summary>
    [Serializable]
    public class RegistryMetaData
    {
        public string registryName;
        public string registrySoGuid;
        public string assetType;
        public int currentRevision;
        public string lastBakedUtc;
    }

    /// <summary>
    /// 루트 폴더의 .regilog JSON 직렬화 모델
    /// </summary>
    [Serializable]
    public class RegistryLogData
    {
        public string registrySoGuid;
        public int currentRevision;
        public List<AssetLogRecord> assets = new();
    }

    /// <summary>
    /// 개별 에셋(GUID)의 변경 이력 레코드
    /// </summary>
    [Serializable]
    public class AssetLogRecord
    {
        public string guid;
        public string lastKnownName;
        public List<AssetRevisionEntry> history = new();
    }

    /// <summary>
    /// 특정 Revision 시점의 에셋 ID 스냅샷 (id == 0 이면 삭제됨을 의미)
    /// </summary>
    [Serializable]
    public class AssetRevisionEntry
    {
        public int rev;
        public ulong id;
        public string date;
    }
}