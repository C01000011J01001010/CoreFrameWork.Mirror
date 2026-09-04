using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;

namespace CoreEditor.EditorTools
{
    public class PrefabComponentInjectorTool : BaseTransactionToolWindow
    {
        // 씬에 임시로 부착된 컴포넌트들을 추적하기 위한 데이터 구조체
        private struct InjectedData
        {
            public GameObject Target;
            public List<Component> AddedComponents;
        }

        // 입력 데이터
        private GameObject _targetParent;
        private GameObject _presetObject;

        // 임시 추가된 컴포넌트 추적 리스트
        private List<InjectedData> _injectedDataTracker = new List<InjectedData>();

        [MenuItem(Constants.ToolRootComponentPrefab + "Prefab Component Injector")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabComponentInjectorTool>("Prefab Injector");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        protected override string GetToolName()
        {
            return "프리팹 컴포넌트 일괄 주입 툴 (Live Preview)";
        }

        protected override void DrawInputArea()
        {
            EditorGUI.BeginDisabledGroup(_currentState != TransactionState.Idle);
            _targetParent = (GameObject)EditorGUILayout.ObjectField("부모 객체 (Target Parent)", _targetParent, typeof(GameObject), true);
            _presetObject = (GameObject)EditorGUILayout.ObjectField("프리셋 객체 (Preset)", _presetObject, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawGuidanceArea()
        {
            EditorGUILayout.HelpBox("💡 [안내] '확인'을 누르면 씬의 인스턴스에 임시로 컴포넌트가 부착됩니다. 인스펙터에서 결과를 확인한 후 '확정'을 누르면 원본 프리팹에 저장됩니다.", MessageType.Info);
        }

        // 🎯 1. 확인 (프리뷰 상태 진입)
        protected override void OnAnalyze()
        {
            _injectedDataTracker.Clear();

            if (_targetParent == null || _presetObject == null)
            {
                EditorUtility.DisplayDialog("경고", "부모 객체와 Preset 객체를 모두 할당해 주세요.", "확인");
                return;
            }

            Component[] presetComponents = _presetObject.GetComponents<Component>();
            List<Component> validPresetComponents = new List<Component>();

            foreach (var comp in presetComponents)
            {
                if (comp == null)
                {
                    _globalErrorMessage = "Preset 오브젝트 내부에 'Missing Script'가 존재합니다! 인스펙터 창에서 해결 후 다시 시도해주세요.";
                    return;
                }
                if (comp is Transform) continue;
                validPresetComponents.Add(comp);
            }

            if (validPresetComponents.Count == 0) return;

            Transform parentTransform = _targetParent.transform;
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                GameObject child = parentTransform.GetChild(i).gameObject;

                if (!PrefabUtility.IsPartOfPrefabInstance(child))
                {
                    _errorObjects.Add(new ToolErrorData { TargetObject = child, Reason = "프리팹 인스턴스가 아님" });
                    continue;
                }

                if (PrefabUtility.GetPrefabAssetType(child) == PrefabAssetType.Model)
                {
                    _errorObjects.Add(new ToolErrorData { TargetObject = child, Reason = "FBX/Model 원본에는 주입 불가" });
                    continue;
                }

                bool hasAllComponents = true;
                bool hasAnyMissing = false;

                foreach (var presetComp in validPresetComponents)
                {
                    if (child.GetComponent(presetComp.GetType()) == null)
                    {
                        hasAllComponents = false;
                        hasAnyMissing = true;
                    }
                }

                if (hasAllComponents)
                {
                    _skippedObjects.Add(child);
                }
                else if (hasAnyMissing)
                {
                    // 🌟 [핵심 변경점] 확인을 누르는 즉시 씬의 객체에 컴포넌트를 복사해서 붙여버립니다!
                    InjectedData data = new InjectedData { Target = child, AddedComponents = new List<Component>() };

                    foreach (Component sourceComp in validPresetComponents)
                    {
                        Type compType = sourceComp.GetType();
                        if (child.GetComponent(compType) != null) continue; // 이미 있으면 패스

                        ComponentUtility.CopyComponent(sourceComp);
                        try
                        {
                            if (ComponentUtility.PasteComponentAsNew(child))
                            {
                                // 방금 붙인 새 컴포넌트 찾기 (보통 맨 마지막에 붙음)
                                Component[] currentComps = child.GetComponents<Component>();
                                Component newlyAddedComp = currentComps[currentComps.Length - 1];
                                data.AddedComponents.Add(newlyAddedComp);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[PrefabInjector] '{child.name}'에 임시 주입 중 실패: {ex.Message}");
                        }
                    }

                    _injectedDataTracker.Add(data);
                    _pendingObjects.Add(child);
                    EditorUtility.SetDirty(child); // 하이라키에 변화가 있음을 유니티에 알림
                }
            }
        }

        // 🎯 2. 확정 (원본 프리팹에 오버라이드 적용)
        protected override void OnCommit()
        {
            if (_injectedDataTracker.Count == 0) return;

            int successCount = 0;

            foreach (InjectedData data in _injectedDataTracker)
            {
                string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(data.Target);
                bool isTargetModified = false;

                foreach (Component addedComp in data.AddedComponents)
                {
                    if (addedComp != null && !string.IsNullOrEmpty(prefabAssetPath))
                    {
                        // 미리 붙여둔 컴포넌트를 원본 에셋에 영구 적용
                        PrefabUtility.ApplyAddedComponent(addedComp, prefabAssetPath, InteractionMode.AutomatedAction);
                        isTargetModified = true;
                    }
                }

                if (isTargetModified) successCount++;
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("트랜잭션 완료", $"프리팹 원본에 최종 적용 완료! \n업데이트된 프리팹 수: {successCount}개", "확인");

            // 확정이 끝났으므로 추적 리스트 초기화 (이후 Cancel이 호출되더라도 지울게 없도록)
            _injectedDataTracker.Clear();
        }

        // 🎯 3. 취소 (씬에 임시로 붙였던 컴포넌트들 롤백)
        protected override void OnCancel()
        {
            if (_injectedDataTracker.Count == 0) return;

            foreach (InjectedData data in _injectedDataTracker)
            {
                foreach (Component comp in data.AddedComponents)
                {
                    if (comp != null)
                    {
                        // 씬에 임시로 붙여놨던 컴포넌트를 강제로 파괴하여 롤백
                        DestroyImmediate(comp);
                    }
                }

                if (data.Target != null)
                {
                    EditorUtility.SetDirty(data.Target);
                }
            }

            _injectedDataTracker.Clear();
            Debug.Log("[PrefabInjector] 임시로 부착되었던 컴포넌트들을 씬에서 모두 제거했습니다.");
        }
    }
}