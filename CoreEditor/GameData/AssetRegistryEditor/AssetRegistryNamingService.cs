using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.GameData
{
    /// <summary>
    /// 에셋 파일의 2-Phase 일괄 리네임 및 B+Tree 폴더의 Bottom-Up 이름 변경을 전담합니다.
    /// </summary>
    public class AssetRegistryNamingService
    {
        public void ApplyBatchIdsToLeaf(IReadOnlyList<AssetInfo> orderedAssets, ulong startId)
        {
            List<(AssetInfo asset, ulong newId)> plan = new(orderedAssets.Count);
            for (int i = 0; i < orderedAssets.Count; i++)
            {
                plan.Add((orderedAssets[i], startId + (ulong)i));
            }

            ExecuteTwoPhaseAssetRename(plan);
        }

        public void ExecuteStrideAllocation(
            IReadOnlyList<FolderNode> activeLeaves,
            IReadOnlyDictionary<string, List<AssetInfo>> customOrderedLists,
            ulong baseStartId,
            ulong stride)
        {
            List<(AssetInfo asset, ulong newId)> totalPlan = new();

            for (int i = 0; i < activeLeaves.Count; i++)
            {
                FolderNode leaf = activeLeaves[i];
                ulong folderStartId = baseStartId + (ulong)i * stride;

                if (!customOrderedLists.TryGetValue(leaf.CanonicalKey, out List<AssetInfo> orderedAssets)
                    || orderedAssets == null
                    || orderedAssets.Count != leaf.AllTargetAssets.Count)
                {
                    orderedAssets = leaf.AllTargetAssets
                        .OrderBy(a => a.HasId ? 0 : 1)
                        .ThenBy(a => a.Id)
                        .ThenBy(a => a.NameWithoutId, StringComparer.Ordinal)
                        .ToList();
                }

                for (int j = 0; j < orderedAssets.Count; j++)
                {
                    totalPlan.Add((orderedAssets[j], folderStartId + (ulong)j));
                }
            }

            ExecuteTwoPhaseAssetRename(totalPlan);
        }

        public void RevokeId(AssetInfo asset)
        {
            string targetName = asset.NameWithoutId;
            if (AssetNamingUtility.TryParseIdPrefix(targetName, out _, out _))
            {
                targetName = $"NEW_{targetName}";
            }

            AssetDatabase.RenameAsset(asset.AssetPath, targetName);
            AssetDatabase.SaveAssets();
        }

        public void MoveAssetToTrash(AssetInfo asset)
        {
            AssetDatabase.MoveAssetToTrash(asset.AssetPath);
        }

        /// <summary>
        /// 가장 깊은 하위(Leaf) 폴더부터 루트 직전까지 역순(Bottom-Up)으로
        /// 폴더명을 '{SubtreeMinId}_{CleanName}' 포맷으로 갱신합니다.
        /// </summary>
        public void RenameTreeFoldersBottomUp(RegistryScanReport report)
        {
            if (report == null) return;

            List<FolderNode> renameTargets = report.AllNodes
                .Where(n => !n.IsRoot && n.NeedsRename)
                .OrderByDescending(n => n.Depth)
                .ToList();

            if (renameTargets.Count == 0) return;

            foreach (var node in renameTargets)
            {
                string expectedName = node.ExpectedFolderName;
                string parentDir = Path.GetDirectoryName(node.FolderPath)?.Replace('\\', '/');
                string targetPath = $"{parentDir}/{expectedName}";

                if (AssetDatabase.IsValidFolder(targetPath))
                {
                    Debug.LogError($"[AssetRegistry] 동일한 이름의 폴더가 이미 존재하여 폴더명을 변경할 수 없습니다: {targetPath}");
                    continue;
                }

                string error = AssetDatabase.RenameAsset(node.FolderPath, expectedName);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[AssetRegistry] 폴더 이름 변경 실패 ({node.FolderPath} -> {expectedName}): {error}");
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 동일 폴더 내 파일명 충돌을 막기 위해 임시 이름을 거치는 2단계 일괄 리네임
        /// </summary>
        public void ExecuteTwoPhaseAssetRename(IReadOnlyList<(AssetInfo asset, ulong newId)> plan)
        {
            if (plan == null || plan.Count == 0) return;

            List<(string tempPath, string finalName)> phase2List = new(plan.Count);

            AssetDatabase.StartAssetEditing();
            try
            {
                // Phase 1: 임시 파일명으로 변경
                foreach (var (asset, newId) in plan)
                {
                    string cleanName = asset.NameWithoutId.TrimStart('_');
                    string finalName = $"{newId}_{cleanName}";

                    if (Path.GetFileNameWithoutExtension(asset.AssetPath) == finalName)
                        continue;

                    string tempName = $"__TMP_{asset.Guid}_{cleanName}";
                    string err = AssetDatabase.RenameAsset(asset.AssetPath, tempName);
                    if (string.IsNullOrEmpty(err))
                    {
                        string tempPath = $"{asset.FolderPath}/{tempName}{asset.Extension}";
                        phase2List.Add((tempPath, finalName));
                    }
                    else
                    {
                        Debug.LogError($"[AssetRegistry] 임시 파일명 변경 실패 ({asset.AssetPath}): {err}");
                    }
                }

                // Phase 2: 최종 '{newId}_{cleanName}'으로 변경
                foreach (var (tempPath, finalName) in phase2List)
                {
                    string err = AssetDatabase.RenameAsset(tempPath, finalName);
                    if (!string.IsNullOrEmpty(err))
                    {
                        Debug.LogError($"[AssetRegistry] 최종 파일명 변경 실패 ({tempPath} -> {finalName}): {err}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
        }
    }
}