using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEditor.GameData
{
    public static class AssetRegistryConstants
    {
        public const string GroupSeparator = " ▸ ";
    }

    public class AssetInfo
    {
        public ulong Id;
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
        public string CanonicalKey;
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

            // 💡 [추가] 상위 폴더에서 하위 서브트리의 상태를 요약 표시하기 위한 집계 프로퍼티
        public int SubtreeLeafCount => IsLeaf ? 1 : Children.Sum(c => c.SubtreeLeafCount);
        public int SubtreeTotalAssetCount => IsLeaf ? AllTargetAssets.Count : Children.Sum(c => c.SubtreeTotalAssetCount);
        public int SubtreeMissingIdCount => IsLeaf ? MissingIdAssets.Count : Children.Sum(c => c.SubtreeMissingIdCount);

        public string ExpectedFolderName => IsRoot
            ? FolderName
            : (HasValidIds ? $"{SubtreeMinId}_{CleanName}" : CleanName);

        public bool NeedsRename => !IsRoot && FolderName != ExpectedFolderName;
    }

    public class RangeConflictInfo
    {
        public string ParentDisplayPath;
        public FolderNode LeftNode;
        public FolderNode RightNode;
    }

    public class RegistryScanReport
    {
        public bool IsValidDirectory;
        public string BaseDirectory;

        // .regimeta 상태 및 소유권 정보
        public bool HasRegiMeta;
        public int CurrentRevision;
        public bool HasOwnerMismatch;
        public string OwnerRegistryName;
        public string OwnerRegistryGuid;

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
            (HasOwnerMismatch ? 1 : 0) +
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
      /// 파일 및 폴더명의 '{ulong}_{Name}' 규칙 파싱 및 임시 접두사 복구 유틸리티
    /// </summary>
    public static class AssetNamingUtility
    {
        private const string TempPrefix = "__TMP_";

        /// <summary>
            /// 이전 작업 중단으로 남아있는 '__TMP_{GUID}_' 접두사가 있다면 제거하고 원래 이름을 반환합니다.
        /// </summary>
        public static string StripTempPrefix(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith(TempPrefix, StringComparison.Ordinal))
                return name;

            int secondUnderscoreAfterTmp = name.IndexOf('_', TempPrefix.Length);
            if (secondUnderscoreAfterTmp > TempPrefix.Length)
            {
                return name.Substring(secondUnderscoreAfterTmp + 1);
            }

            return name;
        }

        public static bool TryParseIdPrefix(string name, out ulong id, out string cleanName)
        {
            id = 0;
            cleanName = StripTempPrefix(name);

            if (string.IsNullOrEmpty(cleanName)) return false;

            int underscoreIdx = cleanName.IndexOf('_');
            if (underscoreIdx > 0 && ulong.TryParse(cleanName.Substring(0, underscoreIdx), out ulong parsedId))
            {
                id = parsedId;
                cleanName = cleanName.Substring(underscoreIdx + 1);
                return true;
            }

            return false;
        }
    }
}