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
    /// .regimeta 및 .regilog 파일의 읽기/쓰기, 소유권 검증, Revision 스냅샷 기록을 전담합니다.
    /// </summary>
    public class AssetRegistryLogService
    {
        public string GetMetaFilePath(string baseDir) =>
            Path.Combine(GetAbsoluteFolderPath(baseDir), AssetRegistryLogConstants.MetaFileName);

        public string GetLogFilePath(string baseDir) =>
            Path.Combine(GetAbsoluteFolderPath(baseDir), AssetRegistryLogConstants.LogFileName);

        /// <summary>
        /// 스캔 단계에서 기존 .regimeta를 읽어 현재 Revision과 소유권 충돌 여부를 진단합니다.
        /// </summary>
        public void InspectMetaState(string baseDir, string currentSoGuid, RegistryScanReport report)
        {
            string metaPath = GetMetaFilePath(baseDir);
            if (!File.Exists(metaPath))
            {
                report.HasRegiMeta = false;
                report.CurrentRevision = 0;
                return;
            }

            try
            {
                string json = File.ReadAllText(metaPath);
                RegistryMetaData meta = JsonUtility.FromJson<RegistryMetaData>(json);
                if (meta == null) return;

                report.HasRegiMeta = true;
                report.CurrentRevision = meta.currentRevision;
                report.OwnerRegistryName = meta.registryName;
                report.OwnerRegistryGuid = meta.registrySoGuid;

                if (!string.IsNullOrEmpty(meta.registrySoGuid)
                    && !string.IsNullOrEmpty(currentSoGuid)
                    && !string.Equals(meta.registrySoGuid, currentSoGuid, StringComparison.Ordinal))
                {
                    report.HasOwnerMismatch = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AssetRegistry] .regimeta 파싱 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 동기화(Sync & Bake) 완료 시점에 호출되어 변경 사항이 있을 때만 새 Revision을 기록합니다.
        /// </summary>
        public int CommitSyncLog(
            Object targetRegistry,
            string registrySoGuid,
            Type assetType,
            RegistryScanReport report)
        {
            string baseDir = report.BaseDirectory;
            string metaPath = GetMetaFilePath(baseDir);
            string logPath = GetLogFilePath(baseDir);
            string nowUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            RegistryLogData logData = LoadOrCreateLogData(logPath, registrySoGuid);

            // 빠른 조회를 위해 GUID 기준으로 기존 레코드 매핑
            Dictionary<string, AssetLogRecord> recordMap = logData.assets
                .Where(r => !string.IsNullOrEmpty(r.guid))
                .ToDictionary(r => r.guid, r => r, StringComparer.Ordinal);

            HashSet<string> activeGuids = new(StringComparer.Ordinal);
            bool hasAnyIdDiff = false;
            int nextRev = logData.currentRevision + 1;

            // 1. 현재 유효 에셋들의 신규 추가 및 ID 변경 감지
            foreach (var asset in report.ValidAssets)
            {
                activeGuids.Add(asset.Guid);
                string cleanDisplayName = $"{asset.NameWithoutId}{asset.Extension}";

                if (!recordMap.TryGetValue(asset.Guid, out AssetLogRecord record))
                {
                    record = new AssetLogRecord
                    {
                        guid = asset.Guid,
                        lastKnownName = cleanDisplayName
                    };
                    record.history.Add(new AssetRevisionEntry
                    {
                        rev = nextRev,
                        id = asset.Id,
                        date = nowUtc
                    });

                    recordMap[asset.Guid] = record;
                    hasAnyIdDiff = true;
                }
                else
                {
                    record.lastKnownName = cleanDisplayName;
                    ulong lastRecordedId = record.history.Count > 0
                        ? record.history[record.history.Count - 1].id
                        : 0ul;

                    if (lastRecordedId != asset.Id)
                    {
                        record.history.Add(new AssetRevisionEntry
                        {
                            rev = nextRev,
                            id = asset.Id,
                            date = nowUtc
                        });
                        hasAnyIdDiff = true;
                    }
                }
            }

            // 2. 이전에 존재했으나 현재 폴더에서 삭제(또는 제외)된 에셋 감지 -> id: 0 기록
            foreach (var kvp in recordMap)
            {
                if (activeGuids.Contains(kvp.Key)) continue;

                AssetLogRecord record = kvp.Value;
                ulong lastRecordedId = record.history.Count > 0
                    ? record.history[record.history.Count - 1].id
                    : 0ul;

                // 직전 상태가 살아있는 에셋(id > 0)이었다면 삭제(id = 0) 이력 추가
                if (lastRecordedId > 0)
                {
                    record.history.Add(new AssetRevisionEntry
                    {
                        rev = nextRev,
                        id = 0ul,
                        date = nowUtc
                    });
                    hasAnyIdDiff = true;
                }
            }

            // 3. 실제 변경점이 발생한 경우에만 Revision 번호 증가
            if (hasAnyIdDiff || logData.currentRevision == 0)
            {
                logData.currentRevision = nextRev;
            }

            logData.registrySoGuid = registrySoGuid;
            // Git Diff가 항상 일정하도록 GUID 오름차순으로 정렬하여 저장
            logData.assets = recordMap.Values.OrderBy(r => r.guid, StringComparer.Ordinal).ToList();

            var metaData = new RegistryMetaData
            {
                registryName = targetRegistry.name,
                registrySoGuid = registrySoGuid,
                assetType = assetType != null ? assetType.FullName : "UnityEngine.Object",
                currentRevision = logData.currentRevision,
                lastBakedUtc = nowUtc
            };

            WriteJsonAtomically(metaPath, JsonUtility.ToJson(metaData, true));
            WriteJsonAtomically(logPath, JsonUtility.ToJson(logData, true));

            return logData.currentRevision;
        }

        private static RegistryLogData LoadOrCreateLogData(string logPath, string registrySoGuid)
        {
            if (File.Exists(logPath))
            {
                try
                {
                    string json = File.ReadAllText(logPath);
                    RegistryLogData loaded = JsonUtility.FromJson<RegistryLogData>(json);
                    if (loaded != null)
                    {
                        loaded.assets ??= new List<AssetLogRecord>();
                        return loaded;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetRegistry] 기존 .regilog 로드 실패. 새로 생성합니다: {ex.Message}");
                }
            }

            return new RegistryLogData
            {
                registrySoGuid = registrySoGuid,
                currentRevision = 0,
                assets = new List<AssetLogRecord>()
            };
        }

        private static void WriteJsonAtomically(string targetPath, string content)
        {
            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempPath, targetPath);
        }

        private static string GetAbsoluteFolderPath(string unityRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, unityRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}