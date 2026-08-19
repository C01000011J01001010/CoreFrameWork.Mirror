using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace CoreEngine.EditorTools
{
    public class UniversalPresetApplierWindow : EditorWindow
    {
        public List<DefaultAsset> targetFolders = new List<DefaultAsset>();
        public Preset targetPreset;
        public PresetOverrideRuleSO activeRule;

        private SerializedObject _so;
        private SerializedProperty _foldersProp;

        [MenuItem("Tools/Core System/Universal Preset Applier")]
        public static void ShowWindow()
        {
            var window = GetWindow<UniversalPresetApplierWindow>("Preset Applier");
            window.minSize = new Vector2(400, 360);
            window.Show();
        }

        private void OnEnable()
        {
            _so = new SerializedObject(this);
            _foldersProp = _so.FindProperty(nameof(targetFolders));
        }

        private void OnGUI()
        {
            _so.Update();

            GUILayout.Label("범용 에셋 프리셋 다중 폴더 적용 툴", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1. 다중 폴더 지정 UI
            EditorGUILayout.PropertyField(_foldersProp, new GUIContent("Target Folders (적용할 폴더들)"), true);
            EditorGUILayout.Space();

            // 2. 프리셋 원본 및 조건 SO 지정 UI
            GUILayout.Label("Target Preset (프리셋 원본)");
            targetPreset = (Preset)EditorGUILayout.ObjectField(targetPreset, typeof(Preset), false);
            EditorGUILayout.Space();

            GUILayout.Label("Active Rule (프리셋 처리 조건)");
            activeRule = (PresetOverrideRuleSO)EditorGUILayout.ObjectField(activeRule, typeof(PresetOverrideRuleSO), false);

            _so.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            // 🌟 실시간 타입 검증 피드백 UI
            DrawTypeCompatibilityFeedback();

            EditorGUILayout.Space(10);

            // 실행 버튼
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("목록의 모든 폴더에 프리셋 적용", GUILayout.Height(40)))
            {
                ApplyPresetToFolders();
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// 에디터 창에서 Preset과 activeRule의 타입 호환성을 실시간으로 가이드해 주는 UI
        /// </summary>
        private void DrawTypeCompatibilityFeedback()
        {
            if (targetPreset != null && activeRule != null)
            {
                if (activeRule.IsCompatibleWithPreset(targetPreset))
                {
                    EditorGUILayout.HelpBox($"[타입 동기화 성공] Preset({targetPreset.GetTargetTypeName()})과 activeRule({activeRule.TargetTypeName})의 타입이 일치합니다.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"[타입 불일치 경고] Preset 타입({targetPreset.GetTargetTypeName()})과 activeRule 타입({activeRule.TargetTypeName})이 다릅니다!", MessageType.Error);
                }
            }
        }

        private void ApplyPresetToFolders()
        {
            // ==========================================
            // 🛡️ 3대 필수 조건 검증 (Guard Clauses)
            // ==========================================

            // [조건 1] 최소 1개 이상의 유효한 폴더가 포함되어 있는가?
            List<string> folderPaths = new List<string>();
            foreach (var folder in targetFolders)
            {
                if (folder != null)
                {
                    string path = AssetDatabase.GetAssetPath(folder);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        folderPaths.Add(path);
                    }
                }
            }

            if (folderPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("경고", "[조건 1 실패] 최소 1개 이상의 유효한 대상 폴더를 리스트에 지정해주세요.", "확인");
                return;
            }

            // [조건 2] 프리셋 원본이 지정되어 있는가?
            if (targetPreset == null)
            {
                EditorUtility.DisplayDialog("경고", "[조건 2 실패] 적용할 Preset 원본 파일을 지정해주세요.", "확인");
                return;
            }

            // [조건 3-1] 프리셋 처리 조건(activeRule)이 지정되어 있는가?
            if (activeRule == null)
            {
                EditorUtility.DisplayDialog("경고", "[조건 3 실패] 프리셋 처리 조건(Active Rule SO)을 지정해주세요.", "확인");
                return;
            }

            // [조건 3-2] 프리셋 타입과 조건 SO의 타입이 서로 일치하는가?
            if (!activeRule.IsCompatibleWithPreset(targetPreset))
            {
                string presetType = targetPreset.GetTargetTypeName();
                string ruleType = activeRule.TargetTypeName;
                EditorUtility.DisplayDialog("경고", $"[조건 3 실패] Preset 타입과 Active Rule 타입이 일치하지 않습니다!\n\n- Preset 타입: {presetType}\n- Active Rule 타겟 타입: {ruleType}", "확인");
                return;
            }

            // ==========================================
            // 🚀 조건 통과 후 실제 실행 프로세스
            // ==========================================
            string[] guids = AssetDatabase.FindAssets("", folderPaths.ToArray());

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "지정된 폴더에 처리할 에셋이 없습니다.", "확인");
                return;
            }

            int appliedCount = 0;
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (AssetDatabase.IsValidFolder(assetPath)) continue;

                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                    if (importer != null)
                    {
                        EditorUtility.DisplayProgressBar("프리셋 적용 중...", $"Processing: {Path.GetFileName(assetPath)}", (float)i / guids.Length);

                        // 룰 SO 내부에서 캐싱/복구 및 주입 실행
                        if (activeRule.TryApplyWithRule(importer, targetPreset))
                        {
                            importer.SaveAndReimport();
                            appliedCount++;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("완료", $"총 {appliedCount}개의 에셋에 성공적으로 프리셋 규칙을 적용했습니다.", "확인");
        }
    }
}