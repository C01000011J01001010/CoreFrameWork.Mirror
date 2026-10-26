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

        public List<AssetInfo> AllTargetAssets = new();
        public List<AssetInfo> ValidIdAssets = new();
        public List<AssetInfo> MissingIdAssets = new();

        public bool HasValidIds;
        public ulong SubtreeMinId = ulong.MaxValue;
        public ulong SubtreeMaxId = 0;

        public ulong AllowedMinId = 1ul;
        public ulong AllowedMaxId = ulong.MaxValue;

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