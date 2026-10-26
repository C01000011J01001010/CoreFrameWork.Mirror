using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using CoreEngine.GameData;
using Object = UnityEngine.Object;

namespace CoreEditor.GameData
{
    /// <summary>
    /// BaseAssetRegistry<TAsset>의 커스텀 인스펙터 뷰(UI) 클래스
    /// </summary>
    [CustomEditor(typeof(_AssetRegistry), true)]
    public class AssetRegistryEditor : Editor
    {
        #region UI 전용 상태 모델
        private class LeafFolderUIState
        {
            public bool IsExpanded;
            public bool FullReindexMode;
            public string StartIdInput = "";
            public List<AssetInfo> WorkingList = new();
            public ReorderableList ReorderableList;
        }
        #endregion

        #region 도메인 서비스 & 상태 필드
        private readonly AssetRegistryScanner _scanner = new();
        private readonly AssetRegistryValidator _validator = new();
        private readonly AssetRegistryNamingService _namingService = new();
        private readonly AssetRegistryAddressableBaker _baker = new();
        private readonly AssetRegistryLogService _logService = new();

        private Type _assetType;
        private string _targetSoGuid;
        private RegistryScanReport _report = new();

        private readonly Dictionary<string, LeafFolderUIState> _leafUIStates = new();
        private bool _showStrideTool;
        private string _strideStartIdInput = "1000";
        private string _strideStepInput = "100";
        #endregion

        #region 생명주기
        private void OnEnable()
        {
            _targetSoGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));

            Type current = target.GetType();
            while (current != null && (!current.IsGenericType || current.GetGenericTypeDefinition() != typeof(BaseAssetRegistry<>)))
            {
                current = current.BaseType;
            }
            if (current != null) _assetType = current.GetGenericArguments()[0];

            Refresh();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            string revBadge = _report.HasRegiMeta ? $" (Rev {_report.CurrentRevision})" : " (Unbaked)";
            EditorGUILayout.LabelField($"📇 Asset Registry [{_assetType?.Name ?? "Unknown"}]{revBadge}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawTopSettings();
            EditorGUILayout.Space();

            DrawSummaryBox();
            EditorGUILayout.Space();

            DrawStrideAllocationSection();
            EditorGUILayout.Space();

            DrawErrorResolutionSection();
            EditorGUILayout.Space();

            DrawLeafFoldersManagementSection();
            EditorGUILayout.Space();

            DrawSyncButton();

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region 상태 갱신 및 워크플로우 중재
        private void Refresh()
        {
            SerializedProperty baseDirProp = serializedObject.FindProperty("_baseDirectory");
            string baseDir = baseDirProp != null ? baseDirProp.stringValue : string.Empty;

            _report = _scanner.Scan(baseDir, _assetType);
            if (_report.IsValidDirectory)
            {
                _logService.InspectMetaState(_report.BaseDirectory, _targetSoGuid, _report);
            }

            SyncLeafUIStates();
            Repaint();
        }

        private void ExecuteFullSyncWorkflow()
        {
            // 1. 폴더명 Bottom-Up 갱신 후 변경된 경로로 재스캔
            _namingService.RenameTreeFoldersBottomUp(_report);
            Refresh();

            // 2. 레지스트리 데이터 베이킹 및 어드레서블 그룹 동기화/정렬
            _baker.BakeAndSync(serializedObject, target, _report);

            // 3. .regimeta 및 .regilog 스냅샷 기록 (실제 변경 시에만 Revision +1)
            int committedRev = _logService.CommitSyncLog(target, _targetSoGuid, _assetType, _report);
            Refresh();

            Debug.Log($"[AssetRegistry] .regimeta & .regilog 기록 완료 (Current Revision: {committedRev})");
        }

        private void ExecuteStrideWorkflow(List<FolderNode> activeLeaves, ulong baseStartId, ulong stride)
        {
            Dictionary<string, List<AssetInfo>> customOrders = new();
            foreach (var leaf in activeLeaves)
            {
                if (_leafUIStates.TryGetValue(leaf.CanonicalKey, out LeafFolderUIState uiState)
                    && uiState.FullReindexMode
                    && uiState.WorkingList.Count == leaf.AllTargetAssets.Count)
                {
                    customOrders[leaf.CanonicalKey] = uiState.WorkingList;
                }
            }

            _namingService.ExecuteStrideAllocation(activeLeaves, customOrders, baseStartId, stride);
            Refresh();
            _namingService.RenameTreeFoldersBottomUp(_report);
            Refresh();
        }
        #endregion

        #region UI 렌더링: 상단 설정 & 요약
        private void DrawTopSettings()
        {
            EditorGUILayout.BeginVertical("box");

            SerializedProperty baseDirProp = serializedObject.FindProperty("_baseDirectory");
            if (baseDirProp == null)
            {
                EditorGUILayout.HelpBox("'_baseDirectory' 프로퍼티를 찾을 수 없습니다. (코드 변경 후 컴파일 중일 수 있습니다)", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // 전체 경로 대신 가장 마지막 폴더명만 추출하여 읽기 전용으로 표시 (툴팁으로 전체 경로 제공)
            string fullPath = baseDirProp.stringValue;
            string folderName = string.IsNullOrEmpty(fullPath)
                ? "(설정되지 않음)"
                : Path.GetFileName(fullPath.TrimEnd('/', '\\'));

            GUI.enabled = false;
            EditorGUILayout.TextField(
                new GUIContent("📁 관리 대상 루트 폴더", fullPath),
                folderName);
            GUI.enabled = true;

            // 🎯 인스펙터 이동 없이 프로젝트 뷰에서 해당 루트 폴더만 핑(Ping)
            if (GUILayout.Button(new GUIContent("🎯", "프로젝트 창에서 폴더 위치 핑(Ping)"), GUILayout.Width(30)))
            {
                if (!string.IsNullOrEmpty(fullPath))
                {
                    Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
                    if (folderObj != null)
                    {
                        EditorGUIUtility.PingObject(folderObj);
                    }
                    else
                    {
                        Debug.LogWarning($"[AssetRegistry] 경로에 해당하는 폴더를 찾을 수 없습니다: {fullPath}");
                    }
                }
            }

            // 📂 폴더 선택 창 열기
            if (GUILayout.Button(new GUIContent("📂", "관리할 루트 폴더 변경"), GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("관리할 에셋 폴더 선택", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    baseDirProp.stringValue = "Assets" + path.Substring(Application.dataPath.Length);
                    serializedObject.ApplyModifiedProperties();
                    Refresh();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 대상 폴더 트리 재스캔", GUILayout.Height(25)))
            {
                serializedObject.ApplyModifiedProperties();
                Refresh();
                GUIUtility.ExitGUI();
            }

            int renameNeededCount = _report.RenameNeededFolderCount;
            GUI.enabled = renameNeededCount > 0
                          && !_report.HasOwnerMismatch
                          && _report.RangeConflicts.Count == 0
                          && _report.NonLeafAssetsViolation.Count == 0;

            if (GUILayout.Button($"🏷️ 폴더명 {renameNeededCount}건 즉시 갱신", GUILayout.Height(25), GUILayout.Width(170)))
            {
                _namingService.RenameTreeFoldersBottomUp(_report);
                Refresh();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSummaryBox()
        {
            EditorGUILayout.LabelField("📊 B+Tree 스캔 결과 요약", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            DrawSummaryLine("🔖 현재 기록된 Revision (.regilog)", _report.CurrentRevision, Color.cyan, "Rev ");
            DrawSummaryLine("✅ 정상 등록 대기 에셋", _report.ValidAssets.Count, Color.green);
            DrawSummaryLine("🌿 단말(Leaf) 폴더 수", _report.LeafNodes.Count, Color.cyan);
            DrawSummaryLine("🚫 잘못된 파일 타입", _report.InvalidTypeAssets.Count, Color.red);
            DrawSummaryLine("⛔ 단일 에셋 정책 위반", _report.MultipleAssetsViolation.Count, Color.red);
            DrawSummaryLine("🌿 비단말 폴더 에셋 배치 위반", _report.NonLeafAssetsViolation.Count, Color.red);
            DrawSummaryLine("❌ ID 중복 충돌", _report.ConflictGroups.Count, Color.red);
            DrawSummaryLine("🔀 폴더 간 ID 대역 교차 위반", _report.RangeConflicts.Count, Color.red);
            DrawSummaryLine("⚠️ ID 누락 (신규 에셋)", _report.MissingIdAssets.Count, Color.yellow);

            EditorGUILayout.EndVertical();
        }

        private static void DrawSummaryLine(string label, int count, Color color, string prefix = "")
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(210));
            GUI.contentColor = count > 0 ? color : Color.white;
            EditorGUILayout.LabelField(string.IsNullOrEmpty(prefix) ? $"{count}건" : $"{prefix}{count}");
            GUI.contentColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region UI 렌더링: Stride 일괄 배분 도구
        private void DrawStrideAllocationSection()
        {
            if (_report.HasOwnerMismatch) return;

            List<FolderNode> activeLeaves = _report.LeafNodes.Where(l => l.AllTargetAssets.Count > 0).ToList();
            if (activeLeaves.Count == 0) return;

            EditorGUILayout.BeginVertical("box");
            _showStrideTool = EditorGUILayout.Foldout(_showStrideTool, "📐 [도구] 전체 단말 폴더 블록 간격(Stride) 일괄 배분", true, EditorStyles.foldoutHeader);

            if (_showStrideTool)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "모든 단말(Leaf) 폴더의 에셋들을 트리 순서에 맞춰 고정 간격(Stride) 블록으로 일괄 재할당합니다.\n" +
                    "폴더 내 에셋 순서는 각 단말 폴더 리스트에 정렬된 순서를 그대로 따릅니다.",
                    MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("시작 기준 ID", GUILayout.Width(90));
                _strideStartIdInput = EditorGUILayout.TextField(_strideStartIdInput, GUILayout.Width(100));
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("폴더당 예약 간격 (Stride)", GUILayout.Width(150));
                _strideStepInput = EditorGUILayout.TextField(_strideStepInput, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                bool isStrideValid = _validator.ValidateStrideAllocation(
                    activeLeaves,
                    _strideStartIdInput,
                    _strideStepInput,
                    out ulong baseStartId,
                    out ulong stride,
                    out string strideError);

                EditorGUILayout.Space(4);
                if (!isStrideValid)
                {
                    EditorGUILayout.HelpBox(strideError, MessageType.Error);
                }
                else
                {
                    EditorGUILayout.BeginVertical("helpBox");
                    EditorGUILayout.LabelField("📋 폴더별 배분 대역 프리뷰", EditorStyles.miniBoldLabel);
                    for (int i = 0; i < activeLeaves.Count; i++)
                    {
                        FolderNode leaf = activeLeaves[i];
                        ulong folderStart = baseStartId + (ulong)i * stride;
                        ulong folderEnd = folderStart + (ulong)(leaf.AllTargetAssets.Count - 1);
                        EditorGUILayout.LabelField(
                            $" • {leaf.CanonicalKey} ({leaf.AllTargetAssets.Count}개) ➔ [{folderStart} ~ {folderEnd}] (여유: {stride - (ulong)leaf.AllTargetAssets.Count}개)",
                            EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }

                GUI.enabled = isStrideValid && _report.NonLeafAssetsViolation.Count == 0;
                if (GUILayout.Button("⚡ 전체 단말 폴더 Stride 일괄 배분 및 폴더명 갱신 실행", GUILayout.Height(28)))
                {
                    int totalAssets = activeLeaves.Sum(l => l.AllTargetAssets.Count);
                    if (EditorUtility.DisplayDialog(
                        "Stride 일괄 배분 확인",
                        $"총 {activeLeaves.Count}개의 단말 폴더({totalAssets}개 에셋)에\n" +
                        $"시작 ID {baseStartId}부터 {stride} 간격으로 ID를 일괄 재부여하시겠습니까?",
                        "실행", "취소"))
                    {
                        ExecuteStrideWorkflow(activeLeaves, baseStartId, stride);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region UI 렌더링: 에러 및 정책 위반 표시
        private void DrawErrorResolutionSection()
        {
            if (_report.StructuralErrorCount == 0) return;

            EditorGUILayout.LabelField("🚨 구조 및 정책 위반 항목", EditorStyles.boldLabel);

            // 0. .regimeta 레지스트리 소유권 충돌
            if (_report.HasOwnerMismatch)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("[🔒 레지스트리 소유권 충돌] 다른 레지스트리 에셋이 관리 중인 폴더입니다!");
                GUI.contentColor = Color.white;
                EditorGUILayout.HelpBox(
                    $"이 폴더의 .regimeta에는 '{_report.OwnerRegistryName}' (GUID: {_report.OwnerRegistryGuid})가 소유자로 등록되어 있습니다.\n" +
                    "올바른 폴더를 다시 선택하거나, 기존 레지스트리를 폐기한 경우에만 루트 폴더의 .regimeta 파일을 수동 삭제하세요.",
                    MessageType.Error);
                EditorGUILayout.EndVertical();
            }

            // 1. 타입 불일치 에셋
            if (_report.InvalidTypeAssets.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField($"[🚫 잘못된 타입 발견] 내부에 {_assetType?.Name} 에셋이 존재하지 않습니다.");
                GUI.contentColor = Color.white;

                foreach (var asset in _report.InvalidTypeAssets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" └─ {asset.PathDetail}", EditorStyles.miniLabel);
                    if (GUILayout.Button("🎯 핑", GUILayout.Width(50))) PingAsset(asset.AssetPath);
                    if (GUILayout.Button("🗑️ 삭제", GUILayout.Width(65)))
                    {
                        _namingService.MoveAssetToTrash(asset);
                        Refresh();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            // 2. 단일 파일 다중 에셋 정책 위반
            if (_report.MultipleAssetsViolation.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("[⛔ 정책 위반] 1파일 1에셋 원칙 위배! (예: 분할된 스프라이트 시트 불가)");
                GUI.contentColor = Color.white;

                foreach (var asset in _report.MultipleAssetsViolation)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" └─ {asset.PathDetail} ({asset.TargetAssetCount}개 포함됨)", EditorStyles.miniLabel);
                    if (GUILayout.Button("🎯 핑", GUILayout.Width(50))) PingAsset(asset.AssetPath);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            // 3. 비단말 폴더 에셋 배치 위반
            if (_report.NonLeafAssetsViolation.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("[🌿 B+Tree 위반] 하위 폴더가 존재하는 비단말(Internal) 폴더에는 에셋을 둘 수 없습니다.");
                GUI.contentColor = Color.white;

                foreach (var asset in _report.NonLeafAssetsViolation)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" └─ {asset.PathDetail}", EditorStyles.miniLabel);
                    if (GUILayout.Button("🎯 핑", GUILayout.Width(50))) PingAsset(asset.AssetPath);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            // 4. ID 중복 충돌
            if (_report.ConflictGroups.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("[❌ ID 중복 충돌] 동일한 ID를 가진 에셋들이 존재합니다.");
                GUI.contentColor = Color.white;

                foreach (var group in _report.ConflictGroups)
                {
                    EditorGUILayout.LabelField($" ID: {group.Id} 중복", EditorStyles.boldLabel);
                    foreach (var asset in group.Assets)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"   └─ {asset.PathDetail}", EditorStyles.miniLabel);
                        if (GUILayout.Button("🎯 핑", GUILayout.Width(50))) PingAsset(asset.AssetPath);
                        if (GUILayout.Button("🔄 ID 회수", GUILayout.Width(80)))
                        {
                            _namingService.RevokeId(asset);
                            Refresh();
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            // 5. 폴더 간 ID 대역 교차 위반
            if (_report.RangeConflicts.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("[🔀 B+Tree 대역 교차 위반] 폴더 간 ID 대역(Min ~ Max)이 서로 겹쳐 트리 정렬이 깨졌습니다.");
                GUI.contentColor = Color.white;

                foreach (var conflict in _report.RangeConflicts)
                {
                    EditorGUILayout.LabelField(
                        $" └─ [{conflict.ParentDisplayPath}] '{conflict.LeftNode.FolderName}' [{conflict.LeftNode.SubtreeMinId}~{conflict.LeftNode.SubtreeMaxId}] " +
                        $"↔ '{conflict.RightNode.FolderName}' [{conflict.RightNode.SubtreeMinId}~{conflict.RightNode.SubtreeMaxId}]",
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.HelpBox("아래 단말 폴더 관리에서 '폴더 전체 재정렬'로 대역을 분리하거나 상단의 'Stride 일괄 배분'을 실행하세요.", MessageType.Warning);
                EditorGUILayout.EndVertical();
            }
        }
        #endregion

        #region UI 렌더링: 단말 폴더별 ReorderableList 관리
        private void DrawLeafFoldersManagementSection()
        {
            if (_report.HasOwnerMismatch || _report.LeafNodes.Count == 0) return;

            EditorGUILayout.LabelField("🗂️ 단말(Leaf) 폴더별 에셋 순서 및 일괄 ID 관리", EditorStyles.boldLabel);

            foreach (var leaf in _report.LeafNodes)
            {
                if (leaf.AllTargetAssets.Count == 0) continue;

                LeafFolderUIState uiState = GetOrCreateLeafUIState(leaf);
                bool hasMissing = leaf.MissingIdAssets.Count > 0;

                EditorGUILayout.BeginVertical("box");

                string rangeText = leaf.HasValidIds ? $"[{leaf.SubtreeMinId} ~ {leaf.SubtreeMaxId}]" : "[ID 미할당]";
                string missingBadge = hasMissing ? $" ⚠️ 신규 {leaf.MissingIdAssets.Count}건" : " ✅ 완료";
                string renameBadge = leaf.NeedsRename ? $" (예정 폴더명: {leaf.ExpectedFolderName})" : "";

                EditorGUILayout.BeginHorizontal();
                GUI.contentColor = hasMissing ? Color.yellow : Color.white;
                uiState.IsExpanded = EditorGUILayout.Foldout(
                    uiState.IsExpanded,
                    $"📁 {leaf.CanonicalKey}  {rangeText}  (총 {leaf.AllTargetAssets.Count}개){missingBadge}{renameBadge}",
                    true,
                    EditorStyles.foldoutHeader);
                GUI.contentColor = Color.white;

                if (GUILayout.Button(new GUIContent("🎯", "이 단말 폴더 위치 핑(Ping)"), GUILayout.Width(28)))
                {
                    PingAsset(leaf.FolderPath);
                }
                EditorGUILayout.EndHorizontal();

                if (uiState.IsExpanded)
                {
                    EditorGUILayout.Space(4);
                    DrawLeafAllowedRangeLabel(leaf);

                    EditorGUILayout.BeginHorizontal();
                    bool newMode = GUILayout.Toolbar(
                        uiState.FullReindexMode ? 1 : 0,
                        new[] { $"➕ 신규 에셋 이어붙이기 ({leaf.MissingIdAssets.Count}건)", $"🔄 폴더 전체 재정렬 ({leaf.AllTargetAssets.Count}건)" }) == 1;

                    if (newMode != uiState.FullReindexMode)
                    {
                        uiState.FullReindexMode = newMode;
                        RebuildLeafWorkingList(leaf, uiState);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(4);

                    if (uiState.WorkingList.Count == 0)
                    {
                        EditorGUILayout.HelpBox("이 폴더에는 ID가 누락된 신규 에셋이 없습니다. 기존 에셋의 순서를 바꾸거나 번호를 다시 매기려면 '🔄 폴더 전체 재정렬' 탭을 선택하세요.", MessageType.Info);
                    }
                    else
                    {
                        DrawLeafBatchControls(leaf, uiState);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private static void DrawLeafAllowedRangeLabel(FolderNode leaf)
        {
            string maxSlotStr = leaf.AllowedMaxId == ulong.MaxValue ? "무제한" : leaf.AllowedMaxId.ToString();
            if (leaf.AllowedMinId <= leaf.AllowedMaxId)
            {
                ulong availableCount = leaf.AllowedMaxId == ulong.MaxValue
                    ? ulong.MaxValue
                    : (leaf.AllowedMaxId - leaf.AllowedMinId + 1ul);
                string availStr = availableCount == ulong.MaxValue ? "무제한" : $"{availableCount}개 가능";
                EditorGUILayout.LabelField($"🔒 현재 위치 안전 허용 대역: {leaf.AllowedMinId} ~ {maxSlotStr} ({availStr})", EditorStyles.miniLabel);
            }
            else
            {
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("🔒 인접 폴더와 대역이 역전/충돌 중입니다. 시작 ID를 새로 지정해 재정렬하세요.", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }
        }

        private void DrawLeafBatchControls(FolderNode leaf, LeafFolderUIState uiState)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("시작 ID (Start ID)", GUILayout.Width(110));
            uiState.StartIdInput = EditorGUILayout.TextField(uiState.StartIdInput, GUILayout.Width(110));

            if (GUILayout.Button("추천값", GUILayout.Width(55)))
            {
                uiState.StartIdInput = _validator.GetDefaultStartIdForLeaf(leaf, uiState.FullReindexMode).ToString();
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("가나다순", GUILayout.Width(68)))
            {
                uiState.WorkingList.Sort((a, b) => string.CompareOrdinal(a.NameWithoutId, b.NameWithoutId));
            }
            if (GUILayout.Button("기존ID순", GUILayout.Width(68)))
            {
                uiState.WorkingList.Sort((a, b) =>
                {
                    if (a.HasId && b.HasId) return a.Id.CompareTo(b.Id);
                    if (a.HasId) return -1;
                    if (b.HasId) return 1;
                    return string.CompareOrdinal(a.NameWithoutId, b.NameWithoutId);
                });
            }
            if (GUILayout.Button("역순", GUILayout.Width(45)))
            {
                uiState.WorkingList.Reverse();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            uiState.ReorderableList?.DoLayoutList();

            bool canApply = _validator.ValidateLeafBatchAssignment(
                _report,
                leaf,
                uiState.FullReindexMode,
                uiState.StartIdInput,
                uiState.WorkingList.Count,
                out ulong parsedStartId,
                out ulong calculatedEndId,
                out string validationMsg);

            if (!canApply)
            {
                EditorGUILayout.HelpBox(validationMsg, MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"✅ 검증 통과: {uiState.WorkingList.Count}개 에셋에 [{parsedStartId} ~ {calculatedEndId}] 대역이 안전하게 부여됩니다.",
                    MessageType.None);
            }

            GUI.enabled = canApply;
            string applyBtnLabel = uiState.FullReindexMode
                ? $"💾 폴더 전체 ({uiState.WorkingList.Count}개) [{parsedStartId} ~ {calculatedEndId}] 일괄 재부여"
                : $"✨ 신규 에셋 ({uiState.WorkingList.Count}개) [{parsedStartId} ~ {calculatedEndId}] 일괄 부여";

            if (GUILayout.Button(applyBtnLabel, GUILayout.Height(28)))
            {
                _namingService.ApplyBatchIdsToLeaf(uiState.WorkingList, parsedStartId);
                uiState.StartIdInput = "";
                Refresh();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }

        private void DrawSyncButton()
        {
            bool canSync = _report.CanSync;

            GUI.enabled = canSync;
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = canSync ? Color.cyan : Color.gray;

            string btnText = canSync
                ? "🚀 B+Tree 폴더명 동기화 · 어드레서블 베이킹 · .regilog 기록"
                : $"🔒 미해결 항목({_report.TotalIssueCount}건) 해결 후 동기화 가능";

            if (GUILayout.Button(btnText, GUILayout.Height(40)))
            {
                ExecuteFullSyncWorkflow();
                GUIUtility.ExitGUI();
            }

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }
        #endregion

        #region ReorderableList 바인딩 & 핑 헬퍼
        private void SyncLeafUIStates()
        {
            foreach (var leaf in _report.LeafNodes)
            {
                LeafFolderUIState state = GetOrCreateLeafUIState(leaf);
                if (leaf.MissingIdAssets.Count > 0 && !state.IsExpanded)
                {
                    state.IsExpanded = true;
                }
                RebuildLeafWorkingList(leaf, state);
            }
        }

        private LeafFolderUIState GetOrCreateLeafUIState(FolderNode leaf)
        {
            if (!_leafUIStates.TryGetValue(leaf.CanonicalKey, out LeafFolderUIState state))
            {
                state = new LeafFolderUIState
                {
                    IsExpanded = leaf.MissingIdAssets.Count > 0,
                    FullReindexMode = leaf.MissingIdAssets.Count == 0
                };
                _leafUIStates[leaf.CanonicalKey] = state;
            }
            return state;
        }

        private void RebuildLeafWorkingList(FolderNode leaf, LeafFolderUIState state)
        {
            List<AssetInfo> sourceList = state.FullReindexMode ? leaf.AllTargetAssets : leaf.MissingIdAssets;

            Dictionary<string, int> prevOrder = new();
            for (int i = 0; i < state.WorkingList.Count; i++)
            {
                prevOrder[state.WorkingList[i].Guid] = i;
            }

            state.WorkingList = sourceList
                .OrderBy(a => prevOrder.TryGetValue(a.Guid, out int idx) ? idx : int.MaxValue)
                .ThenBy(a => a.HasId ? 0 : 1)
                .ThenBy(a => a.Id)
                .ThenBy(a => a.NameWithoutId, StringComparer.Ordinal)
                .ToList();

            if (string.IsNullOrEmpty(state.StartIdInput) || !ulong.TryParse(state.StartIdInput, out _))
            {
                state.StartIdInput = _validator.GetDefaultStartIdForLeaf(leaf, state.FullReindexMode).ToString();
            }

            state.ReorderableList = new ReorderableList(state.WorkingList, typeof(AssetInfo), true, true, false, false)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "드래그하여 부여 순서 변경 (기존 파일명 ➔ 부여 후 예상 파일명)");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index < 0 || index >= state.WorkingList.Count) return;
                    AssetInfo item = state.WorkingList[index];
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    bool hasValidStart = ulong.TryParse(state.StartIdInput, out ulong startId) && startId > 0;
                    string previewName = hasValidStart && (ulong.MaxValue - startId >= (ulong)index)
                        ? $"{startId + (ulong)index}_{item.NameWithoutId}{item.Extension}"
                        : $"?_{item.NameWithoutId}{item.Extension}";

                    Rect leftRect = new Rect(rect.x, rect.y, rect.width - 55, rect.height);
                    Rect btnRect = new Rect(rect.x + rect.width - 50, rect.y, 50, rect.height - 2);

                    EditorGUI.LabelField(leftRect, $"{index + 1}. {item.FileName}  ➔  {previewName}", EditorStyles.miniLabel);
                    if (GUI.Button(btnRect, "🎯 핑", EditorStyles.miniButton))
                    {
                        PingAsset(item.AssetPath);
                    }
                }
            };
        }

        /// <summary>
        /// 인스펙터 포커스(Selection)를 바꾸지 않고 프로젝트 뷰에서 위치만 핑(Ping)합니다.
        /// </summary>
        private static void PingAsset(string assetPath)
        {
            Object obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
            }
        }
        #endregion
    }
}