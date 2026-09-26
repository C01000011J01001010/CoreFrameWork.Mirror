using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using CoreEngine.GameData;
using Object = UnityEngine.Object;

namespace CoreEditor.GameData
{
    [CustomEditor(typeof(_AssetRegistry), true)]
    public class AssetRegistryEditor : Editor
    {
        private Type _assetType;
        private string _targetGuid;

        // 스캔 결과 분류
        private List<AssetInfo> _validAssets = new();
        private List<AssetInfo> _invalidTypeAssets = new();
        private List<AssetInfo> _multipleAssetsViolation = new();
        private List<AssetInfo> _missingIdAssets = new();
        private List<ConflictGroup> _conflictGroups = new();

        // UI 상태
        private int _selectedAddressableGroupIndex = 0;
        private List<AddressableAssetGroup> _addressableGroups = new();
        private string[] _addressableGroupNames;

        private Dictionary<string, string> _manualIdInputs = new();
        private Dictionary<string, int> _recommendedSelections = new();
        private List<int> _recommendedIds = new();
        private string[] _recommendedIdLabels = new string[0];

        private void OnEnable()
        {
            _targetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));

            Type current = target.GetType();
            while (current != null && (!current.IsGenericType || current.GetGenericTypeDefinition() != typeof(BaseAssetRegistry<>)))
            {
                current = current.BaseType;
            }
            if (current != null) _assetType = current.GetGenericArguments()[0];

            LoadAddressableGroups();
            Refresh();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"📇 Asset Registry [{_assetType?.Name ?? "Unknown"}]", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawTopSettings();
            EditorGUILayout.Space();

            DrawSummaryBox();
            EditorGUILayout.Space();

            DrawActionRequiredSection();
            EditorGUILayout.Space();

            DrawSyncButton();

            serializedObject.ApplyModifiedProperties();
        }

        // =========================================================
        // [UI 렌더링 파트]
        // =========================================================

        private void DrawTopSettings()
        {
            EditorGUILayout.BeginVertical("box");

            SerializedProperty baseDirProp = serializedObject.FindProperty("_baseDirectory");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(baseDirProp, new GUIContent("📁 관리 대상 폴더"));
            if (GUILayout.Button("📂", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("관리할 에셋 폴더 선택", Application.dataPath, "");
                if (path.StartsWith(Application.dataPath))
                {
                    baseDirProp.stringValue = "Assets" + path.Substring(Application.dataPath.Length);
                    Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_addressableGroupNames != null && _addressableGroupNames.Length > 0)
            {
                _selectedAddressableGroupIndex = EditorGUILayout.Popup("📦 Addressable 그룹", _selectedAddressableGroupIndex, _addressableGroupNames);
                EditorPrefs.SetInt($"RegGroup_{_targetGuid}", _selectedAddressableGroupIndex);
            }
            else
            {
                EditorGUILayout.HelpBox("Addressables 패키지가 설치되지 않았거나 초기화되지 않았습니다.", MessageType.Error);
            }

            if (GUILayout.Button("🔄 대상 폴더 재스캔", GUILayout.Height(25)))
            {
                Refresh();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSummaryBox()
        {
            EditorGUILayout.LabelField("📊 스캔 결과 요약", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            DrawSummaryLine("✅ 정상 등록 대기", _validAssets.Count, Color.green);
            DrawSummaryLine("🚫 잘못된 파일 타입", _invalidTypeAssets.Count, Color.red);
            DrawSummaryLine("⛔ 단일 에셋 정책 위반", _multipleAssetsViolation.Count, Color.red);
            DrawSummaryLine("❌ ID 중복 충돌", _conflictGroups.Count, Color.red);
            DrawSummaryLine("⚠️ ID 누락 (신규)", _missingIdAssets.Count, Color.yellow);

            EditorGUILayout.EndVertical();
        }

        private void DrawSummaryLine(string label, int count, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            GUI.contentColor = count > 0 ? color : Color.white;
            EditorGUILayout.LabelField($"{count}건");
            GUI.contentColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionRequiredSection()
        {
            if (_invalidTypeAssets.Count == 0 && _multipleAssetsViolation.Count == 0 && _conflictGroups.Count == 0 && _missingIdAssets.Count == 0) return;

            EditorGUILayout.LabelField("🚨 해결 필요 항목", EditorStyles.boldLabel);

            if (_invalidTypeAssets.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField($"[🚫 잘못된 타입 발견] 내부에 {_assetType.Name} 에셋이 존재하지 않습니다.");
                GUI.contentColor = Color.white;

                foreach (var asset in _invalidTypeAssets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" └─ {asset.FileName}", EditorStyles.miniLabel);
                    if (GUILayout.Button("🗑️ 에셋 삭제", GUILayout.Width(80)))
                    {
                        AssetDatabase.MoveAssetToTrash(asset.AssetPath);
                        Refresh();
                        GUIUtility.ExitGUI(); // [Fix] UI 그리기 즉시 중단 및 다음 프레임에 새로고침
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            if (_multipleAssetsViolation.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField($"[⛔ 정책 위반] 1파일 1에셋 원칙 위배! (예: 스프라이트 시트 불가)");
                GUI.contentColor = Color.white;

                foreach (var asset in _multipleAssetsViolation)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" └─ {asset.FileName} ({asset.TargetAssetCount}개의 {_assetType.Name} 포함됨)", EditorStyles.miniLabel);
                    if (GUILayout.Button("선택", GUILayout.Width(50)))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(asset.AssetPath);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            if (_conflictGroups.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("[❌ ID 중복 충돌] 동일한 ID를 가진 파일들이 존재합니다.");
                GUI.contentColor = Color.white;

                foreach (var group in _conflictGroups)
                {
                    EditorGUILayout.LabelField($" ID: {group.Id} 중복", EditorStyles.boldLabel);
                    foreach (var asset in group.Assets)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"   └─ {asset.FileName}", EditorStyles.miniLabel);
                        if (GUILayout.Button("🔄 ID 회수", GUILayout.Width(80)))
                        {
                            RevokeId(asset);
                            GUIUtility.ExitGUI(); // [Fix] 이중 루프 탈출 및 UI 그리기 중단
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (_missingIdAssets.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField("[⚠️ 신규/누락 에셋] 파일명에 ID가 없습니다. 명시적으로 ID를 부여하세요.");
                GUI.contentColor = Color.white;

                foreach (var asset in _missingIdAssets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" └─ {asset.FileName}", EditorStyles.miniLabel, GUILayout.Width(150));

                    if (!_manualIdInputs.ContainsKey(asset.AssetPath)) _manualIdInputs[asset.AssetPath] = "";
                    _manualIdInputs[asset.AssetPath] = EditorGUILayout.TextField(_manualIdInputs[asset.AssetPath], GUILayout.Width(100));

                    if (GUILayout.Button("💾 직접 적용", GUILayout.Width(80)))
                    {
                        if (int.TryParse(_manualIdInputs[asset.AssetPath], out int newId))
                        {
                            ApplyNewId(asset, newId);
                            GUIUtility.ExitGUI(); // [Fix]
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (_recommendedIdLabels.Length > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(150, false);

                        GUI.contentColor = Color.yellow;
                        EditorGUILayout.LabelField("💡 추천 ID:", GUILayout.Width(60));
                        GUI.contentColor = Color.white;

                        if (!_recommendedSelections.ContainsKey(asset.AssetPath))
                            _recommendedSelections[asset.AssetPath] = 0;

                        if (_recommendedSelections[asset.AssetPath] >= _recommendedIdLabels.Length)
                            _recommendedSelections[asset.AssetPath] = 0;

                        _recommendedSelections[asset.AssetPath] = EditorGUILayout.Popup(
                            _recommendedSelections[asset.AssetPath],
                            _recommendedIdLabels,
                            GUILayout.ExpandWidth(true));

                        if (GUILayout.Button("✨ 선택 적용", GUILayout.Width(80)))
                        {
                            ApplyNewId(asset, _recommendedIds[_recommendedSelections[asset.AssetPath]]);
                            GUIUtility.ExitGUI(); // [Fix]
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(5);
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSyncButton()
        {
            int errorCount = _invalidTypeAssets.Count + _multipleAssetsViolation.Count + _conflictGroups.Count + _missingIdAssets.Count;
            bool canSync = (errorCount == 0) && _validAssets.Count > 0;

            GUI.enabled = canSync;
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = canSync ? Color.cyan : Color.gray;

            string btnText = canSync ? "🚀 레지스트리 갱신 및 Addressable 자동 세팅" : $"🔒 에러({errorCount}) 해결 후 갱신 가능";
            if (GUILayout.Button(btnText, GUILayout.Height(40)))
            {
                ExecuteSyncAndBake();
            }

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }

        // =========================================================
        // [핵심 로직: 서브 에셋 완전 탐색 및 분류]
        // =========================================================

        private void LoadAddressableGroups()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                _addressableGroups = settings.groups.Where(g => !g.ReadOnly).ToList();
                _addressableGroupNames = _addressableGroups.Select(g => g.Name).ToArray();

                int savedIndex = EditorPrefs.GetInt($"RegGroup_{_targetGuid}", 0);
                _selectedAddressableGroupIndex = Mathf.Clamp(savedIndex, 0, _addressableGroupNames.Length - 1);
            }
        }

        private void Refresh()
        {
            _validAssets.Clear();
            _invalidTypeAssets.Clear();
            _multipleAssetsViolation.Clear();
            _missingIdAssets.Clear();
            _conflictGroups.Clear();
            _manualIdInputs.Clear();
            _recommendedSelections.Clear();

            string baseDir = serializedObject.FindProperty("_baseDirectory").stringValue;
            if (string.IsNullOrEmpty(baseDir) || !AssetDatabase.IsValidFolder(baseDir)) return;

            string[] guids = AssetDatabase.FindAssets("", new[] { baseDir });
            Dictionary<int, List<AssetInfo>> idDict = new Dictionary<int, List<AssetInfo>>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;

                var info = new AssetInfo
                {
                    AssetPath = path,
                    FileName = Path.GetFileName(path),
                    PathDetail = path.Substring(baseDir.Length + 1)
                };

                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                int targetAssetCount = 0;

                if (_assetType != null)
                {
                    targetAssetCount = allAssets.Count(a => _assetType.IsInstanceOfType(a));
                }

                info.TargetAssetCount = targetAssetCount;

                if (targetAssetCount == 0)
                {
                    _invalidTypeAssets.Add(info);
                    continue;
                }
                else if (targetAssetCount > 1)
                {
                    _multipleAssetsViolation.Add(info);
                    continue;
                }

                string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
                int underscoreIdx = nameWithoutExt.IndexOf('_');

                if (underscoreIdx > 0 && int.TryParse(nameWithoutExt.Substring(0, underscoreIdx), out int id))
                {
                    info.Id = id;
                    info.NameWithoutId = nameWithoutExt.Substring(underscoreIdx + 1);

                    if (!idDict.ContainsKey(id)) idDict[id] = new List<AssetInfo>();
                    idDict[id].Add(info);
                }
                else
                {
                    info.NameWithoutId = nameWithoutExt;
                    _missingIdAssets.Add(info);
                }
            }

            foreach (var kvp in idDict)
            {
                if (kvp.Value.Count == 1)
                {
                    _validAssets.Add(kvp.Value[0]);
                }
                else
                {
                    _conflictGroups.Add(new ConflictGroup { Id = kvp.Key, Assets = kvp.Value });
                }
            }

            CalculateRecommendedIds();
            Repaint();
        }

        // [핵심 로직 변경] 기획하신 "연속된 ID의 끝 번호만 추천"하는 알고리즘
        private void CalculateRecommendedIds()
        {
            _recommendedIds.Clear();

            // 1. 현재 사용 중인 모든 ID 수집 (충돌 중인 ID도 할당된 것으로 간주하여 방어)
            HashSet<int> usedIds = new HashSet<int>(_validAssets.Select(a => a.Id));
            foreach (var group in _conflictGroups)
            {
                usedIds.Add(group.Id);
            }

            // 2. 아무 ID도 없다면 무조건 1 추천
            if (usedIds.Count == 0)
            {
                _recommendedIds.Add(1);
            }
            else
            {
                // 3. ID들을 오름차순 정렬 후, 연속된 구간의 끝을 찾음
                List<int> sortedIds = usedIds.ToList();
                sortedIds.Sort();

                foreach (int id in sortedIds)
                {
                    // 현재 id의 다음 번호(id + 1)가 사용 중이지 않다면, 거기가 연속된 클러스터의 끝임
                    if (!usedIds.Contains(id + 1))
                    {
                        _recommendedIds.Add(id + 1);
                    }
                }
            }

            _recommendedIdLabels = _recommendedIds.Select(id => id.ToString()).ToArray();
        }

        private void RevokeId(AssetInfo asset)
        {
            AssetDatabase.RenameAsset(asset.AssetPath, asset.NameWithoutId);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        private void ApplyNewId(AssetInfo asset, int newId)
        {
            if (_validAssets.Any(a => a.Id == newId) || _conflictGroups.Any(g => g.Id == newId))
            {
                EditorUtility.DisplayDialog("ID 중복", $"ID {newId}번은 이미 사용중입니다. 다른 ID를 입력하세요.", "확인");
                return;
            }

            string cleanName = asset.NameWithoutId.TrimStart('_');
            string newName = $"{newId}_{cleanName}";

            AssetDatabase.RenameAsset(asset.AssetPath, newName);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        private void ExecuteSyncAndBake()
        {
            SerializedProperty entriesProp = serializedObject.FindProperty("_entries");
            entriesProp.ClearArray();

            string baseDir = serializedObject.FindProperty("_baseDirectory").stringValue;

            for (int i = 0; i < _validAssets.Count; i++)
            {
                entriesProp.InsertArrayElementAtIndex(i);
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Id").intValue = _validAssets[i].Id;
                element.FindPropertyRelative("PathDetail").stringValue = _validAssets[i].PathDetail;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
                return;
            }

            var group = _addressableGroups[_selectedAddressableGroupIndex];

            foreach (var asset in _validAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(asset.AssetPath);
                var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                entry.SetAddress($"{baseDir}/{asset.PathDetail}");
            }

            string registryGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
            var registryEntry = settings.CreateOrMoveEntry(registryGuid, group, readOnly: false, postEvent: false);

            settings.AddLabel("GlobalData");
            registryEntry.SetLabel("GlobalData", true, true);

            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetRegistry] 갱신 완료! {_validAssets.Count}개의 에셋이 Addressable에 등록되었습니다.");
        }

        // =========================================================
        // [내부 데이터 구조]
        // =========================================================

        private class AssetInfo
        {
            public int Id;
            public string AssetPath;
            public string FileName;
            public string NameWithoutId;
            public string PathDetail;
            public int TargetAssetCount;
        }

        private class ConflictGroup
        {
            public int Id;
            public List<AssetInfo> Assets;
        }
    }
}