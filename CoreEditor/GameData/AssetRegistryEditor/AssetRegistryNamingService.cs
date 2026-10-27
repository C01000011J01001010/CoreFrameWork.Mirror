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
            /// 파일명 충돌 여부를 먼저 판별하고, StartAssetEditing 세션을 분리하여 안전하게 일괄 리네임합니다.
        /// </summary>
        public void ExecuteTwoPhaseAssetRename(IReadOnlyList<(AssetInfo asset, ulong newId)> plan)
        {
            if (plan == null || plan.Count == 0) return;

                  // 현재 폴더 내에 존재하는 모든 파일 경로 집합 (대소문자 구분 없이 충돌 검사)
            HashSet<string> existingPaths = new(StringComparer.OrdinalIgnoreCase);
            foreach (var (asset, _) in plan)
            {
                existingPaths.Add(asset.AssetPath);
            }

            List<(AssetInfo asset, string finalName)> directRenameList = new();
            List<(string guid, string assetPath, string tempName, string finalName)> twoPhaseList = new();

            foreach (var (asset, newId) in plan)
            {
                string cleanName = AssetNamingUtility.StripTempPrefix(asset.NameWithoutId).TrimStart('_');
                string finalName = $"{newId}_{cleanName}";

                        // 이미 목표 파일명과 완전히 같다면 건너뜀
                if (Path.GetFileNameWithoutExtension(asset.AssetPath) == finalName)
                    continue;

                string targetPath = $"{asset.FolderPath}/{finalName}{asset.Extension}";

                        // 목표 경로에 이미 다른 에셋 파일이 존재하는 경우에만 2-Phase(__TMP_) 리네임 수행
                bool hasCollision = existingPaths.Contains(targetPath)
                                    || AssetDatabase.LoadMainAssetAtPath(targetPath) != null;

                if (hasCollision)
                {
                    string tempName = $"__TMP_{asset.Guid}_{cleanName}";
                    twoPhaseList.Add((asset.Guid, asset.AssetPath, tempName, finalName));
                }
                else
                {
                    directRenameList.Add((asset, finalName));
                }
            }

                  //1. 충돌이 없는 일반 에셋들은 1-Phase로 즉시 변경 + 충돌 에셋들의 Phase 1(임시 이름 변경) 수행
            if (directRenameList.Count > 0 || twoPhaseList.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var (asset, finalName) in directRenameList)
                    {
                        string err = AssetDatabase.RenameAsset(asset.AssetPath, finalName);
                        if (!string.IsNullOrEmpty(err))
                        {
                            Debug.LogError($"[AssetRegistry] 파일명 변경 실패 ({asset.AssetPath} -> {finalName}): {err}");
                        }
                    }

                    foreach (var item in twoPhaseList)
                    {
                        string err = AssetDatabase.RenameAsset(item.assetPath, item.tempName);
                        if (!string.IsNullOrEmpty(err))
                        {
                            Debug.LogError($"[AssetRegistry] 임시 파일명 변경 실패 ({item.assetPath}): {err}");
                        }
                    }
                }
                finally
                {
                              // StopAssetEditing()이 호출되면서 AssetDatabase 내부 경로 테이블이 최신화됨
                    AssetDatabase.StopAssetEditing();
                }
            }

                  // 2. 충돌 에셋들이 있는 경우, 갱신된 GUID 경로를 조회하여 Phase 2(최종 이름 변경) 수행
            if (twoPhaseList.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var item in twoPhaseList)
                    {
                        string currentTempPath = AssetDatabase.GUIDToAssetPath(item.guid);
                        if (string.IsNullOrEmpty(currentTempPath)) continue;

                        string err = AssetDatabase.RenameAsset(currentTempPath, item.finalName);
                        if (!string.IsNullOrEmpty(err))
                        {
                            Debug.LogError($"[AssetRegistry] 최종 파일명 변경 실패 ({currentTempPath} -> {item.finalName}): {err}");
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}