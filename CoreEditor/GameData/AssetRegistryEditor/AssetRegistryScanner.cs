using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEditor.GameData
{
    /// <summary>
    /// 관리 대상 폴더를 스캔하여 B+Tree 구조 및 에셋 상태를 진단합니다.
    /// </summary>
    public class AssetRegistryScanner
    {
        public RegistryScanReport Scan(string rawBaseDir, Type assetType)
        {
            var report = new RegistryScanReport();
            string baseDir = rawBaseDir?.Replace('\\', '/').TrimEnd('/');

            if (string.IsNullOrEmpty(baseDir) || !AssetDatabase.IsValidFolder(baseDir))
            {
                report.IsValidDirectory = false;
                return report;
            }

            report.IsValidDirectory = true;
            report.BaseDirectory = baseDir;

            // 1. 디렉터리 트리 구성
            Dictionary<string, FolderNode> folderMap = BuildFolderTree(baseDir, report);

            // 2. 에셋 스캔 및 분류
            ScanAndClassifyAssets(baseDir, assetType, folderMap, report);

            // 3. Bottom-Up 서브트리 [MinId, MaxId] 계산 및 자식 노드 정렬
            ComputeSubtreeRangesRecursive(report.RootNode);

            // 4. 형제 서브트리 간 [MinId ~ MaxId] 대역 교차 검사
            DetectRangeConflictsRecursive(report.RootNode, report.RangeConflicts);

            // 5. 정렬된 단말(Leaf) 폴더 수집 및 안전 허용 슬롯 계산
            CollectSortedLeavesRecursive(report.RootNode, report.LeafNodes);
            ComputeAllowedSlotsForLeaves(report.LeafNodes);

            return report;
        }

        private Dictionary<string, FolderNode> BuildFolderTree(string baseDir, RegistryScanReport report)
        {
            Dictionary<string, FolderNode> map = new(StringComparer.Ordinal);
            string baseFolderName = Path.GetFileName(baseDir);
            AssetNamingUtility.TryParseIdPrefix(baseFolderName, out _, out string baseCleanName);

            var rootNode = new FolderNode
            {
                FolderPath = baseDir,
                RelativePath = string.Empty,
                FolderName = baseFolderName,
                CleanName = baseCleanName,
                CanonicalKey = baseCleanName,
                IsRoot = true,
                Depth = 0
            };

            report.RootNode = rootNode;
            map[baseDir] = rootNode;
            report.AllNodes.Add(rootNode);

            string[] folderGuids = AssetDatabase.FindAssets("t:Folder", new[] { baseDir });
            List<string> folderPaths = folderGuids
                .Select(g => AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/'))
                .Where(p => p.Length > baseDir.Length && p.StartsWith(baseDir + "/", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(p => p.Length)
                .ToList();

            foreach (string path in folderPaths)
            {
                string folderName = Path.GetFileName(path);
                AssetNamingUtility.TryParseIdPrefix(folderName, out _, out string cleanName);
                string relPath = path.Substring(baseDir.Length + 1);
                string parentPath = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? baseDir;

                if (!map.TryGetValue(parentPath, out FolderNode parentNode))
                {
                    parentNode = rootNode;
                }

                var node = new FolderNode
                {
                    FolderPath = path,
                    RelativePath = relPath,
                    FolderName = folderName,
                    CleanName = cleanName,
                    CanonicalKey = $"{parentNode.CanonicalKey}{AssetRegistryConstants.GroupSeparator}{cleanName}",
                    IsRoot = false,
                    Depth = parentNode.Depth + 1,
                    Parent = parentNode
                };

                parentNode.Children.Add(node);
                map[path] = node;
                report.AllNodes.Add(node);
            }

            return map;
        }

        private void ScanAndClassifyAssets(
            string baseDir,
            Type assetType,
            Dictionary<string, FolderNode> folderMap,
            RegistryScanReport report)
        {
            string[] allGuids = AssetDatabase.FindAssets("", new[] { baseDir });
            Dictionary<ulong, List<AssetInfo>> idDict = new();

            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (AssetDatabase.IsValidFolder(path)) continue;

                int targetAssetCount = CountTargetAssetsAtPath(path, assetType);
                string folderPath = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? baseDir;

                if (!folderMap.TryGetValue(folderPath, out FolderNode parentFolder))
                {
                    parentFolder = report.RootNode;
                }

                var info = new AssetInfo
                {
                    Guid = guid,
                    AssetPath = path,
                    FolderPath = folderPath,
                    FileName = Path.GetFileName(path),
                    Extension = Path.GetExtension(path),
                    PathDetail = path.Substring(baseDir.Length + 1),
                    TargetAssetCount = targetAssetCount
                };

                if (targetAssetCount == 0)
                {
                    report.InvalidTypeAssets.Add(info);
                    continue;
                }

                if (targetAssetCount > 1)
                {
                    report.MultipleAssetsViolation.Add(info);
                    continue;
                }

                string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
                if (AssetNamingUtility.TryParseIdPrefix(nameWithoutExt, out ulong parsedId, out string cleanAssetName) && parsedId > 0)
                {
                    info.Id = parsedId;
                    info.NameWithoutId = cleanAssetName;
                }
                else
                {
                    info.Id = 0;
                    info.NameWithoutId = nameWithoutExt;
                }

                // B+Tree 규칙 1: 비단말(Internal) 폴더에는 에셋 배치 불가
                if (!parentFolder.IsLeaf)
                {
                    report.NonLeafAssetsViolation.Add(info);
                    continue;
                }

                parentFolder.AllTargetAssets.Add(info);

                if (info.HasId)
                {
                    parentFolder.ValidIdAssets.Add(info);
                    if (!idDict.TryGetValue(info.Id, out var list))
                    {
                        list = new List<AssetInfo>();
                        idDict[info.Id] = list;
                    }
                    list.Add(info);
                }
                else
                {
                    parentFolder.MissingIdAssets.Add(info);
                    report.MissingIdAssets.Add(info);
                }
            }

            foreach (var kvp in idDict)
            {
                if (kvp.Value.Count == 1)
                {
                    report.ValidAssets.Add(kvp.Value[0]);
                }
                else
                {
                    report.ConflictGroups.Add(new ConflictGroup { Id = kvp.Key, Assets = kvp.Value });
                }
            }
        }

        private static int CountTargetAssetsAtPath(string path, Type assetType)
        {
            if (assetType == null) return 0;

            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (mainType == null || mainType == typeof(DefaultAsset) || mainType == typeof(MonoScript))
                return 0;

            if (assetType != typeof(Sprite)
                && assetType.IsAssignableFrom(mainType)
                && (mainType == typeof(AudioClip) || typeof(ScriptableObject).IsAssignableFrom(mainType) || mainType == typeof(Material)))
            {
                return 1;
            }

            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            return allAssets.Count(a => a != null && assetType.IsInstanceOfType(a));
        }

        private void ComputeSubtreeRangesRecursive(FolderNode node)
        {
            if (node.IsLeaf)
            {
                node.ValidIdAssets.Sort((a, b) => a.Id.CompareTo(b.Id));
                node.MissingIdAssets.Sort((a, b) => string.CompareOrdinal(a.NameWithoutId, b.NameWithoutId));

                if (node.ValidIdAssets.Count > 0)
                {
                    node.HasValidIds = true;
                    node.SubtreeMinId = node.ValidIdAssets[0].Id;
                    node.SubtreeMaxId = node.ValidIdAssets[node.ValidIdAssets.Count - 1].Id;
                }
                else
                {
                    node.HasValidIds = false;
                    node.SubtreeMinId = ulong.MaxValue;
                    node.SubtreeMaxId = 0;
                }
                return;
            }

            bool hasAnyValid = false;
            ulong minId = ulong.MaxValue;
            ulong maxId = 0;

            foreach (var child in node.Children)
            {
                ComputeSubtreeRangesRecursive(child);
                if (child.HasValidIds)
                {
                    hasAnyValid = true;
                    if (child.SubtreeMinId < minId) minId = child.SubtreeMinId;
                    if (child.SubtreeMaxId > maxId) maxId = child.SubtreeMaxId;
                }
            }

            node.HasValidIds = hasAnyValid;
            node.SubtreeMinId = minId;
            node.SubtreeMaxId = maxId;

            node.Children.Sort(CompareFolderNodes);
        }

        private static int CompareFolderNodes(FolderNode a, FolderNode b)
        {
            if (a.HasValidIds && b.HasValidIds)
            {
                int cmp = a.SubtreeMinId.CompareTo(b.SubtreeMinId);
                if (cmp != 0) return cmp;
                cmp = a.SubtreeMaxId.CompareTo(b.SubtreeMaxId);
                if (cmp != 0) return cmp;
            }
            else if (a.HasValidIds) return -1;
            else if (b.HasValidIds) return 1;

            return string.CompareOrdinal(a.CleanName, b.CleanName);
        }

        private void DetectRangeConflictsRecursive(FolderNode node, List<RangeConflictInfo> conflicts)
        {
            if (node.IsLeaf) return;

            List<FolderNode> validChildren = node.Children.Where(c => c.HasValidIds).ToList();
            for (int i = 0; i < validChildren.Count - 1; i++)
            {
                FolderNode left = validChildren[i];
                FolderNode right = validChildren[i + 1];

                if (left.SubtreeMaxId >= right.SubtreeMinId)
                {
                    conflicts.Add(new RangeConflictInfo
                    {
                        ParentDisplayPath = node.CanonicalKey,
                        LeftNode = left,
                        RightNode = right
                    });
                }
            }

            foreach (var child in node.Children)
            {
                DetectRangeConflictsRecursive(child, conflicts);
            }
        }

        private void CollectSortedLeavesRecursive(FolderNode node, List<FolderNode> results)
        {
            if (node.IsLeaf)
            {
                results.Add(node);
                return;
            }

            foreach (var child in node.Children)
            {
                CollectSortedLeavesRecursive(child, results);
            }
        }

        private static void ComputeAllowedSlotsForLeaves(List<FolderNode> leafNodes)
        {
            for (int i = 0; i < leafNodes.Count; i++)
            {
                FolderNode current = leafNodes[i];

                ulong allowedMin = 1ul;
                for (int prevIdx = i - 1; prevIdx >= 0; prevIdx--)
                {
                    if (leafNodes[prevIdx].HasValidIds)
                    {
                        allowedMin = leafNodes[prevIdx].SubtreeMaxId == ulong.MaxValue
                            ? ulong.MaxValue
                            : leafNodes[prevIdx].SubtreeMaxId + 1ul;
                        break;
                    }
                }

                ulong allowedMax = ulong.MaxValue;
                for (int nextIdx = i + 1; nextIdx < leafNodes.Count; nextIdx++)
                {
                    if (leafNodes[nextIdx].HasValidIds)
                    {
                        allowedMax = leafNodes[nextIdx].SubtreeMinId > 0
                            ? leafNodes[nextIdx].SubtreeMinId - 1ul
                            : 0ul;
                        break;
                    }
                }

                current.AllowedMinId = allowedMin;
                current.AllowedMaxId = allowedMax;
            }
        }
    }
}