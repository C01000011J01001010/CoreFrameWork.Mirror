using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace CustomTools.Editor
{
    public class PrefabComponentRemoverTool : BaseTransactionToolWindow
    {
        // 입력 데이터
        private GameObject _targetParent;
        private GameObject _presetObject;

        [MenuItem("Tools/Core System/Prefab Component Remover")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabComponentRemoverTool>("Prefab Remover");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        // 1. 툴 이름 정의
        protected override string GetToolName()
        {
            return "프리팹 컴포넌트 일괄 제거 툴 (Live Preview)";
        }

        // 2. 입력 영역 UI 구현
        protected override void DrawInputArea()
        {
            EditorGUI.BeginDisabledGroup(_currentState != TransactionState.Idle);
            _targetParent = (GameObject)EditorGUILayout.ObjectField("부모 객체 (Target Parent)", _targetParent, typeof(GameObject), true);
            _presetObject = (GameObject)EditorGUILayout.ObjectField("프리셋 객체 (Preset)", _presetObject, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();
        }

        // 3. 안내 문구 추가
        protected override void DrawGuidanceArea()
        {
            EditorGUILayout.HelpBox("💡 [안내] '확인'을 누르면 Preset에 등록된 컴포넌트 타입들이 자식 프리팹에서 '임시 삭제(오버라이드)'됩니다. 인스펙터를 확인한 후 '확정'을 눌러주세요.", MessageType.Warning);
        }

        // 4. 확인 (Analyze): 씬의 인스턴스에서 컴포넌트 임시 삭제 (프리뷰 진입)
        protected override void OnAnalyze()
        {
            if (_targetParent == null || _presetObject == null)
            {
                EditorUtility.DisplayDialog("경고", "부모 객체와 Preset 객체를 모두 할당해 주세요.", "확인");
                return;
            }

            Component[] presetComponents = _presetObject.GetComponents<Component>();
            List<Type> targetTypes = new List<Type>();

            // 프리셋 유효성 검사 및 타입 추출
            foreach (var comp in presetComponents)
            {
                if (comp == null)
                {
                    _globalErrorMessage = "Preset 오브젝트 내부에 'Missing Script'가 존재합니다! 인스펙터 창에서 해결 후 다시 시도해주세요.";
                    return;
                }
                if (comp is Transform) continue;
                targetTypes.Add(comp.GetType());
            }

            if (targetTypes.Count == 0) return;

            Transform parentTransform = _targetParent.transform;
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                GameObject child = parentTransform.GetChild(i).gameObject;

                // 공통 엣지케이스 검증
                if (!PrefabUtility.IsPartOfPrefabInstance(child))
                {
                    _errorObjects.Add(new ToolErrorData { TargetObject = child, Reason = "프리팹 인스턴스가 아님" });
                    continue;
                }

                if (PrefabUtility.GetPrefabAssetType(child) == PrefabAssetType.Model)
                {
                    _errorObjects.Add(new ToolErrorData { TargetObject = child, Reason = "FBX/Model 원본에서는 컴포넌트를 삭제할 수 없습니다." });
                    continue;
                }

                // 제거 대상 컴포넌트가 존재치 않는지 체크
                bool hasAnyComponent = false;
                foreach (Type type in targetTypes)
                {
                    if (child.GetComponent(type) != null)
                    {
                        hasAnyComponent = true;
                        break;
                    }
                }

                if (!hasAnyComponent)
                {
                    _skippedObjects.Add(child); // 지울 게 없는 깨끗한 객체들
                }
                else
                {
                    // 🌟 [실시간 프리뷰 실행] 대상 컴포넌트들을 씬 인스턴스에서 Destroy 
                    // 유니티 프리팹 시스템이 자동으로 '제거 오버라이드 상태(파란색 스트라이프)'로 인식합니다.
                    foreach (Type type in targetTypes)
                    {
                        // 중복 컴포넌트가 있을 수 있으므로 모두 찾아 제거
                        Component[] components = child.GetComponents(type);
                        foreach (var comp in components)
                        {
                            if (comp != null)
                            {
                                DestroyImmediate(comp);
                            }
                        }
                    }

                    _pendingObjects.Add(child);
                    EditorUtility.SetDirty(child);
                }
            }
        }

        // 🎯 5. 확정 (Commit): 임시 제거된 상태를 프리팹 원본 파일에 영구 적용
        protected override void OnCommit()
        {
            if (_pendingObjects.Count == 0) return;

            int successCount = 0;

            foreach (GameObject target in _pendingObjects)
            {
                // 최상위 프리팹 루트 객체 획득
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(target);
                if (root == null) continue;

                // 유니티 프리팹 내부 저장소에서 이 객체에 발생한 '제거 오버라이드 찌꺼기'들을 긁어옴
                var removedComponents = PrefabUtility.GetRemovedComponents(root);
                bool isTargetModified = false;

                foreach (var removed in removedComponents)
                {
                    // 이 지워진 컴포넌트의 컨테이너가 현재 타겟 객체와 일치할 때만 원본 프리팹 파일에 Apply 처리
                    if (removed.containingInstanceGameObject == target)
                    {
                        PrefabUtility.ApplyRemovedComponent(target, removed.assetComponent, InteractionMode.AutomatedAction);
                        isTargetModified = true;
                    }
                }

                if (isTargetModified) successCount++;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("트랜잭션 완료", $"프리팹 원본에서 컴포넌트 완전 삭제 완료! \n반영된 프리팹 수: {successCount}개", "확인");
        }

        // 🎯 6. 취소 (Cancel): 임시 제거 오버라이드 상태를 원상 복구(Revert)
        protected override void OnCancel()
        {
            if (_pendingObjects.Count == 0) return;

            foreach (GameObject target in _pendingObjects)
            {
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(target);
                if (root == null) continue;

                var removedComponents = PrefabUtility.GetRemovedComponents(root);

                foreach (var removed in removedComponents)
                {
                    if (removed.containingInstanceGameObject == target)
                    {
                        // 🌟 지워졌던 컴포넌트들의 원본 직렬화 데이터(값 정보 포함)를 프리팹 에셋에서 역추적해 복구(Revert)
                        PrefabUtility.RevertRemovedComponent(target, removed.assetComponent, InteractionMode.AutomatedAction);
                    }
                }
                EditorUtility.SetDirty(target);
            }

            Debug.Log("[PrefabRemover] 임시 삭제되었던 컴포넌트들을 프리팹 원본 데이터를 기반으로 완벽히 복구했습니다.");
        }
    }
}