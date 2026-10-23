using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEditor.GameData
{
    /// <summary>
    /// 레지스트리 직렬화 데이터 베이킹 및 어드레서블 그룹 생성/정리/정렬을 담당합니다.
    /// </summary>
    public class AssetRegistryAddressableBaker
    {
        public void BakeAndSync(
            SerializedObject serializedRegistry,
            Object targetRegistry,
            RegistryScanReport report)
        {
            if (report == null || !report.IsValidDirectory) return;

            // 1. 레지스트리 내부 _entries 리스트에 ID 오름차순으로 베이킹
            BakeRegistryEntries(serializedRegistry, targetRegistry, report);

            // 2. 어드레서블 그룹 동기화 및 정렬
            SyncAddressableGroups(report);
        }

        private void BakeRegistryEntries(
            SerializedObject serializedRegistry,
            Object targetRegistry,
            RegistryScanReport report)
        {
            SerializedProperty entriesProp = serializedRegistry.FindProperty("_entries");
            entriesProp.ClearArray();

            List<AssetInfo> sortedAssets = report.ValidAssets.OrderBy(a => a.Id).ToList();
            for (int i = 0; i < sortedAssets.Count; i++)
            {
                entriesProp.InsertArrayElementAtIndex(i);
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);

                element.FindPropertyRelative("Id").longValue = (long)sortedAssets[i].Id;
                element.FindPropertyRelative("PathDetail").stringValue = sortedAssets[i].PathDetail;
            }

            serializedRegistry.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetRegistry);
        }

        private void SyncAddressableGroups(RegistryScanReport report)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AssetRegistry] Addressable Settings를 찾을 수 없습니다.");
                return;
            }

            string baseDir = report.BaseDirectory;
            string rootCleanName = report.RootNode != null ? report.RootNode.CleanName : Path.GetFileName(baseDir);
            Dictionary<AddressableAssetGroup, ulong> registryGroupsMinId = new();

            // 단말 폴더별로 정규화 이름('루트 ▸ 서브 ▸ 단말') 그룹 생성 및 엔트리 등록
            foreach (var leaf in report.LeafNodes)
            {
                if (!leaf.HasValidIds || leaf.ValidIdAssets.Count == 0) continue;

                string groupName = leaf.CanonicalKey;
                AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
                if (targetGroup == null)
                {
                    targetGroup = settings.CreateGroup(groupName, false, false, false, settings.DefaultGroup.Schemas);
                }

                registryGroupsMinId[targetGroup] = leaf.SubtreeMinId;

                foreach (var asset in leaf.ValidIdAssets)
                {
                    var entry = settings.CreateOrMoveEntry(asset.Guid, targetGroup, readOnly: false, postEvent: false);
                    entry.SetAddress($"{baseDir}/{asset.PathDetail}");
                }
            }

            // 레지스트리 소속 빈 그룹 자동 제거
            CleanupEmptyRegistryGroups(settings, rootCleanName);

            // 각 그룹의 MinId 오름차순으로 Addressable Groups 리스트 정렬
            SortAddressableGroupsByMinId(settings, registryGroupsMinId);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AssetRegistry] 동기화 완료! 총 {report.ValidAssets.Count}개 에셋이 {registryGroupsMinId.Count}개의 단말 그룹에 등록 및 ID 순으로 정렬되었습니다.");
        }

        private static void CleanupEmptyRegistryGroups(AddressableAssetSettings settings, string rootCleanName)
        {
            List<AddressableAssetGroup> emptyGroups = settings.groups
                .Where(g => g != null
                            && !g.IsDefaultGroup()
                            && g.entries.Count == 0
                            && (g.Name == rootCleanName
                                || g.Name.StartsWith(rootCleanName + AssetRegistryConstants.GroupSeparator, StringComparison.Ordinal)
                                || g.Name.StartsWith(rootCleanName + "_", StringComparison.Ordinal)))
                .ToList();

            foreach (var emptyGroup in emptyGroups)
            {
                settings.RemoveGroup(emptyGroup);
            }
        }

        private static void SortAddressableGroupsByMinId(
            AddressableAssetSettings settings,
            Dictionary<AddressableAssetGroup, ulong> registryGroupsMinId)
        {
            if (registryGroupsMinId.Count <= 1) return;

            List<int> slotIndices = new();
            for (int i = 0; i < settings.groups.Count; i++)
            {
                if (settings.groups[i] != null && registryGroupsMinId.ContainsKey(settings.groups[i]))
                {
                    slotIndices.Add(i);
                }
            }

            List<AddressableAssetGroup> sortedGroups = registryGroupsMinId
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            for (int i = 0; i < slotIndices.Count; i++)
            {
                settings.groups[slotIndices[i]] = sortedGroups[i];
            }
        }
    }
}