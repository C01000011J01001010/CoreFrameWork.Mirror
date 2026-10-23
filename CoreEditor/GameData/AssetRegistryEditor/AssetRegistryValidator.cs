using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEditor.GameData
{
    /// <summary>
    /// 일괄 ID 부여 및 Stride 배분 전, 대역 유효성과 B+Tree 무결성을 사전 시뮬레이션합니다.
    /// </summary>
    public class AssetRegistryValidator
    {
        public ulong GetDefaultStartIdForLeaf(FolderNode leaf, bool fullReindexMode)
        {
            if (fullReindexMode)
            {
                return leaf.HasValidIds ? leaf.SubtreeMinId : leaf.AllowedMinId;
            }

            if (leaf.HasValidIds && leaf.SubtreeMaxId < ulong.MaxValue)
            {
                return leaf.SubtreeMaxId + 1ul;
            }

            return leaf.AllowedMinId;
        }

        public bool ValidateLeafBatchAssignment(
            RegistryScanReport report,
            FolderNode targetLeaf,
            bool fullReindexMode,
            string startIdStr,
            int count,
            out ulong startId,
            out ulong endId,
            out string errorMessage)
        {
            startId = 0;
            endId = 0;
            errorMessage = string.Empty;

            if (count <= 0)
            {
                errorMessage = "부여할 대상 에셋이 없습니다.";
                return false;
            }

            if (!ulong.TryParse(startIdStr, out startId) || startId == 0)
            {
                errorMessage = "시작 ID는 1 이상의 유효한 숫자(ulong)여야 합니다.";
                return false;
            }

            ulong offset = (ulong)(count - 1);
            if (ulong.MaxValue - startId < offset)
            {
                errorMessage = "부여 끝 ID가 ulong 최대 범위를 초과합니다.";
                return false;
            }

            endId = startId + offset;

            // 1. 해당 단말 폴더의 변경 후 예상 [SimMin, SimMax] 산출
            ulong targetSimMin = startId;
            ulong targetSimMax = endId;

            if (!fullReindexMode && targetLeaf.HasValidIds)
            {
                foreach (var existing in targetLeaf.ValidIdAssets)
                {
                    if (existing.Id >= startId && existing.Id <= endId)
                    {
                        errorMessage = $"폴더 내 기존 에셋 '{existing.FileName}'(ID: {existing.Id})과 부여 대역 [{startId}~{endId}]이 겹칩니다.";
                        return false;
                    }
                }

                targetSimMin = Math.Min(targetLeaf.SubtreeMinId, startId);
                targetSimMax = Math.Max(targetLeaf.SubtreeMaxId, endId);
            }

            // 2. 다른 단말 폴더들과의 대역 침범 검사
            foreach (var otherLeaf in report.LeafNodes)
            {
                if (otherLeaf == targetLeaf || !otherLeaf.HasValidIds) continue;

                if (targetSimMin <= otherLeaf.SubtreeMaxId && targetSimMax >= otherLeaf.SubtreeMinId)
                {
                    errorMessage = $"다른 단말 폴더 '{otherLeaf.CanonicalKey}'의 대역 [{otherLeaf.SubtreeMinId}~{otherLeaf.SubtreeMaxId}]을 침범합니다.";
                    return false;
                }
            }

            // 3. 상위 서브트리 간 B+Tree 대역 교차 시뮬레이션
            Dictionary<FolderNode, (bool hasValid, ulong min, ulong max)> simRanges = new();
            SimulateTreeRangesRecursive(report.RootNode, targetLeaf, targetSimMin, targetSimMax, simRanges);

            if (!CheckSimulatedTreeNonOverlapping(report.RootNode, simRanges, out string treeConflictMsg))
            {
                errorMessage = treeConflictMsg;
                return false;
            }

            return true;
        }

        public bool ValidateStrideAllocation(
            IReadOnlyList<FolderNode> activeLeaves,
            string startIdStr,
            string strideStr,
            out ulong baseStartId,
            out ulong stride,
            out string errorMessage)
        {
            baseStartId = 0;
            stride = 0;
            errorMessage = string.Empty;

            if (!ulong.TryParse(startIdStr, out baseStartId) || baseStartId == 0)
            {
                errorMessage = "시작 기준 ID는 1 이상의 숫자여야 합니다.";
                return false;
            }

            if (!ulong.TryParse(strideStr, out stride) || stride == 0)
            {
                errorMessage = "폴더당 예약 간격(Stride)은 1 이상의 숫자여야 합니다.";
                return false;
            }

            for (int i = 0; i < activeLeaves.Count; i++)
            {
                FolderNode leaf = activeLeaves[i];
                int assetCount = leaf.AllTargetAssets.Count;

                if (i < activeLeaves.Count - 1 && (ulong)assetCount > stride)
                {
                    errorMessage = $"'{leaf.CanonicalKey}' 폴더의 에셋 수({assetCount}개)가 예약 간격(Stride: {stride})을 초과합니다. Stride를 {assetCount} 이상으로 늘려주세요.";
                    return false;
                }

                if (ulong.MaxValue / stride < (ulong)i)
                {
                    errorMessage = "Stride 배분 계산 중 ulong 범위를 초과했습니다.";
                    return false;
                }

                ulong folderStart = baseStartId + (ulong)i * stride;
                if (ulong.MaxValue - folderStart < (ulong)Math.Max(0, assetCount - 1))
                {
                    errorMessage = "마지막 에셋 ID가 ulong 최대 범위를 초과합니다.";
                    return false;
                }
            }

            return true;
        }

        private void SimulateTreeRangesRecursive(
            FolderNode node,
            FolderNode targetLeaf,
            ulong targetMin,
            ulong targetMax,
            Dictionary<FolderNode, (bool hasValid, ulong min, ulong max)> simMap)
        {
            if (node.IsLeaf)
            {
                simMap[node] = node == targetLeaf
                    ? (true, targetMin, targetMax)
                    : (node.HasValidIds, node.SubtreeMinId, node.SubtreeMaxId);
                return;
            }

            bool hasValid = false;
            ulong min = ulong.MaxValue;
            ulong max = 0;

            foreach (var child in node.Children)
            {
                SimulateTreeRangesRecursive(child, targetLeaf, targetMin, targetMax, simMap);
                var childSim = simMap[child];
                if (childSim.hasValid)
                {
                    hasValid = true;
                    if (childSim.min < min) min = childSim.min;
                    if (childSim.max > max) max = childSim.max;
                }
            }

            simMap[node] = (hasValid, min, max);
        }

        private bool CheckSimulatedTreeNonOverlapping(
            FolderNode node,
            Dictionary<FolderNode, (bool hasValid, ulong min, ulong max)> simMap,
            out string conflictMsg)
        {
            conflictMsg = string.Empty;
            if (node.IsLeaf) return true;

            var validChildren = node.Children
                .Where(c => simMap[c].hasValid)
                .OrderBy(c => simMap[c].min)
                .ThenBy(c => simMap[c].max)
                .ToList();

            for (int i = 0; i < validChildren.Count - 1; i++)
            {
                FolderNode left = validChildren[i];
                FolderNode right = validChildren[i + 1];
                var leftRange = simMap[left];
                var rightRange = simMap[right];

                if (leftRange.max >= rightRange.min)
                {
                    conflictMsg = $"상위 폴더 트리 대역 위반: '{left.CanonicalKey}' [{leftRange.min}~{leftRange.max}] 구간과 " +
                                  $"'{right.CanonicalKey}' [{rightRange.min}~{rightRange.max}] 구간이 서로 교차합니다.";
                    return false;
                }
            }

            foreach (var child in node.Children)
            {
                if (!CheckSimulatedTreeNonOverlapping(child, simMap, out conflictMsg))
                    return false;
            }

            return true;
        }
    }
}