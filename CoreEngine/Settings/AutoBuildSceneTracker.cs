using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CoreEngine.Helpers;
using CoreEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.Settings
{
    [Serializable]
    public class AutoBuildSceneTracker
    {
        public SceneReference globalScene;
        public SceneReference[] extensionSceneList;
        public SceneReference firstScene;

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private List<string> _lastSyncedScenes = new List<string>();

        private ScriptableObject _owner;

        public void Validate(ScriptableObject owner)
        {
            _owner = owner;
            EditorApplication.delayCall -= SyncBuildSettings;
            EditorApplication.delayCall += SyncBuildSettings;
        }

        private void SyncBuildSettings()
        {
            if (_owner == null) return;

            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
            HashSet<string> currentGuids = new HashSet<string>();

            // ==============================================
            // 1. 현재 SO에 등록된 씬들의 'GUID' 취합
            // ==============================================
            if (globalScene != null && !string.IsNullOrEmpty(globalScene.SceneGUID))
                currentGuids.Add(globalScene.SceneGUID);

            if (extensionSceneList != null)
            {
                foreach (var ext in extensionSceneList)
                {
                    if (ext != null && !string.IsNullOrEmpty(ext.SceneGUID))
                        currentGuids.Add(ext.SceneGUID);
                }
            }

            if (firstScene != null && !string.IsNullOrEmpty(firstScene.SceneGUID))
                currentGuids.Add(firstScene.SceneGUID);

            bool isModified = false;

            // ==============================================
            // 2. 철회(Revoke)
            // ==============================================
            foreach (string oldGuid in _lastSyncedScenes)
            {
                if (!currentGuids.Contains(oldGuid))
                {
                    buildScenes.RemoveAll(s => s.guid.ToString() == oldGuid);
                    isModified = true;
                }
            }

            // ==============================================
            // 3. 확정(Commit)
            // ==============================================
            foreach (string newGuid in currentGuids)
            {
                if (!buildScenes.Any(s => s.guid.ToString() == newGuid))
                {
                    string latestPath = AssetDatabase.GUIDToAssetPath(newGuid);
                    buildScenes.Add(new EditorBuildSettingsScene(latestPath, true));
                    isModified = true;
                }
            }

            // ==============================================
            // 4. 안정된 정렬 (Stable Sorting)
            // ==============================================
            List<EditorBuildSettingsScene> sortedScenes = new List<EditorBuildSettingsScene>();

            // 4-1. GlobalScene 우선 배치 (무조건 0번 인덱스 강제)
            if (globalScene != null && !string.IsNullOrEmpty(globalScene.SceneGUID))
            {
                // FindIndex를 사용하여 정확하게 도려내고 참조 에러 원천 차단
                int idx = buildScenes.FindIndex(s => s.guid.ToString() == globalScene.SceneGUID);
                if (idx >= 0)
                {
                    sortedScenes.Add(buildScenes[idx]);
                    buildScenes.RemoveAt(idx);
                }
            }

            // 4-2. Extension Scenes 배열 순서대로 배치
            if (extensionSceneList != null)
            {
                foreach (var ext in extensionSceneList)
                {
                    if (ext != null && !string.IsNullOrEmpty(ext.SceneGUID))
                    {
                        int idx = buildScenes.FindIndex(s => s.guid.ToString() == ext.SceneGUID);
                        if (idx >= 0)
                        {
                            sortedScenes.Add(buildScenes[idx]);
                            buildScenes.RemoveAt(idx);
                        }
                    }
                }
            }

            // 4-3. First Scene은 맨 마지막에 배치 (GlobalScene과 Extension Scenes 뒤)
            if (firstScene != null && !string.IsNullOrEmpty(firstScene.SceneGUID))
            {
                // FindIndex를 사용하여 정확하게 도려내고 참조 에러 원천 차단
                int idx = buildScenes.FindIndex(s => s.guid.ToString() == firstScene.SceneGUID);
                if (idx >= 0)
                {
                    sortedScenes.Add(buildScenes[idx]);
                    buildScenes.RemoveAt(idx);
                }
            }

            // 남은 씬들은 기존 '상대 순서'를 유지하며 뒤에 이어붙임
            sortedScenes.AddRange(buildScenes);

            // 4-4. "순서" 변경 검증
            var currentEditorScenes = EditorBuildSettings.scenes;
            if (currentEditorScenes.Length != sortedScenes.Count)
            {
                isModified = true;
            }
            else
            {
                for (int i = 0; i < sortedScenes.Count; i++)
                {
                    if (currentEditorScenes[i].guid != sortedScenes[i].guid)
                    {
                        isModified = true;
                        break;
                    }
                }
            }

            // ==============================================
            // 5. 최종 반영 및 SO 저장
            // ==============================================
            if (isModified)
            {
                EditorBuildSettings.scenes = sortedScenes.ToArray();
                EditorUtility.SetDirty(_owner); // 변경사항 영구 저장
                LogHelper.Log("[CoreEngine] 빌드 세팅 씬 목록이 GUID 기반으로 안전하게 동기화 및 정렬되었습니다.", LogColor.Cyan);
            }

            // 데이터가 변경되었다면 더티 체킹 후 저장
            bool isTrackingChanged = _lastSyncedScenes.Count != currentGuids.Count || _lastSyncedScenes.Except(currentGuids).Any();
            if (isTrackingChanged)
            {
                _lastSyncedScenes = currentGuids.ToList();
                EditorUtility.SetDirty(_owner); // 추적 리스트 변경사항도 저장
            }
        }
#endif
    }
}