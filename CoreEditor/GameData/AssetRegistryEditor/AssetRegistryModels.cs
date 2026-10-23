using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEditor.GameData
{
    public static class AssetRegistryConstants
    {
        public const string GroupSeparator = " ▸ ";
    }

    /// <summary>
    /// 개별 에셋 파일의 스캔 메타데이터
    /// </summary>
    public class AssetInfo
    {
        public ulong Id; // 0이면 미할당(신규) 에셋
        public bool HasId => Id > 0;
        public string Guid;
        public string AssetPath;
        public string FolderPath;
        public string FileName;
        public string Extension;
        public string NameWithoutId;
        public string PathDetail;
        public int TargetAssetCount;
    }

    /// <summary>
    /// 동일 ID 충돌 그룹
    /// </summary>
    public class ConflictGroup
    {
        public ulong Id;
        public List<AssetInfo> Assets = new();
    }

    /// <summary>
    /// B+Tree 디렉터리 노드 모델
    /// </summary>
    public class FolderNode
    {
        public string FolderPath;
        public string RelativePath;
        public string FolderName;
        public string CleanName;
        public string CanonicalKey; // {min_id}_ 제외된 정규화 경로
        public bool IsRoot;
        public int Depth;

        public FolderNode Parent;
        public List<FolderNode> Children = new();
        public bool IsLeaf => Children.Count == 0;

        // 해당 폴더 직속 타깃 에셋 목록
        public List<AssetInfo> AllTargetAssets = new();
        public List<AssetInfo> ValidIdAssets = new();
        public List<AssetInfo> MissingIdAssets = new();

        // B+Tree 서브트리 대역 정보
        public bool HasValidIds;
        public ulong SubtreeMinId = ulong.MaxValue;
        public ulong SubtreeMaxId = 0;

        // 단말 폴더 전용: 인접 단말 폴더 기준 안전 허용 대역
        public ulong AllowedMinId = 1ul;
        public ulong AllowedMaxId = ulong.MaxValue;

        public string ExpectedFolderName => IsRoot
            ? FolderName
            : (HasValidIds ? $"{SubtreeMinId}_{CleanName}" : CleanName);

        public bool NeedsRename => !IsRoot && FolderName != ExpectedFolderName;
    }

    /// <summary>
    /// 형제 서브트리 간 ID 대역 교차 위반 정보
    /// </summary>
    public class RangeConflictInfo
    {
        public string ParentDisplayPath;
        public FolderNode LeftNode;
        public FolderNode RightNode;
    }

    /// <summary>
    /// 1회 스캔으로 생성되는 전체 진단 리포트
    /// </summary>
    public class RegistryScanReport
    {
        public bool IsValidDirectory;
        public string BaseDirectory;

        public FolderNode RootNode;
        public List<FolderNode> AllNodes = new();
        public List<FolderNode> LeafNodes = new();

        public List<AssetInfo> ValidAssets = new();
        public List<AssetInfo> InvalidTypeAssets = new();
        public List<AssetInfo> MultipleAssetsViolation = new();
        public List<AssetInfo> NonLeafAssetsViolation = new();
        public List<AssetInfo> MissingIdAssets = new();
        public List<ConflictGroup> ConflictGroups = new();
        public List<RangeConflictInfo> RangeConflicts = new();

        public int StructuralErrorCount =>
            InvalidTypeAssets.Count +
            MultipleAssetsViolation.Count +
            NonLeafAssetsViolation.Count +
            ConflictGroups.Count +
            RangeConflicts.Count;

        public int TotalIssueCount => StructuralErrorCount + MissingIdAssets.Count;

        public bool CanSync => IsValidDirectory && TotalIssueCount == 0 && ValidAssets.Count > 0;

        public int RenameNeededFolderCount => AllNodes.Count(n => n.NeedsRename);
    }

    /// <summary>
    /// 파일 및 폴더명의 '{ulong}_{Name}' 규칙 파싱 유틸리티
    /// </summary>
    public static class AssetNamingUtility
    {
        public static bool TryParseIdPrefix(string name, out ulong id, out string cleanName)
        {
            id = 0;
            cleanName = name;

            if (string.IsNullOrEmpty(name)) return false;

            int underscoreIdx = name.IndexOf('_');
            if (underscoreIdx > 0 && ulong.TryParse(name.Substring(0, underscoreIdx), out ulong parsedId))
            {
                id = parsedId;
                cleanName = name.Substring(underscoreIdx + 1);
                return true;
            }

            return false;
        }
    }
}