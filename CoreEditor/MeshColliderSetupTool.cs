using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CustomTools.Editor
{
    public class MeshColliderSetupTool : EditorWindow
    {
        private enum TransactionState
        {
            Idle,       // 대기 상태
            Staged,     // 확인(Stage) 적용 후 확정 대기 상태
            Committed   // 최종 확정 완료 상태
        }

        private GameObject _targetObject;
        private TransactionState _currentState = TransactionState.Idle;

        // 🌟 [핵심] 글로벌 Undo 스택에 의존하지 않는 독립 그림자 상태(Shadow State) 데이터
        private List<MeshCollider> _tempAddedColliders = new List<MeshCollider>();
        private List<Collider> _tempHiddenColliders = new List<Collider>();

        // UI 갱신용 리스트
        private List<GameObject> _processedMeshObjects = new List<GameObject>();
        private List<GameObject> _otherRendererObjects = new List<GameObject>();

        private Vector2 _scrollPositionProcessed;
        private Vector2 _scrollPositionOther;

        [MenuItem("Tools/MeshCollider Setup Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MeshColliderSetupTool>("MeshCollider Setup");
            window.minSize = new Vector2(400, 520);
            window.Show();
        }

        //private void OnSelectionChange()
        //{
        //    // Staged 상태일 때는 타겟 변경 방지 (Lock)
        //    if (_currentState == TransactionState.Staged) return;

        //    if (Selection.activeGameObject != null)
        //    {
        //        _targetObject = Selection.activeGameObject;
        //        Repaint();
        //    }
        //}

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("⚙️ MeshCollider Transaction Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawStateHelpBox();

            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(_currentState == TransactionState.Staged);
            _targetObject = (GameObject)EditorGUILayout.ObjectField("Target Parent", _targetObject, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(15);
            DrawTransactionButtons();
            GUILayout.Space(15);

            // 처리 목록 출력
            if (_processedMeshObjects.Count > 0)
            {
                string prefix = _currentState == TransactionState.Staged ? "[임시 대기]" : "[최종 확정]";
                GUILayout.Label($"{prefix} MeshCollider 반영 대상 ({_processedMeshObjects.Count}개)", EditorStyles.boldLabel);
                _scrollPositionProcessed = EditorGUILayout.BeginScrollView(_scrollPositionProcessed, "box", GUILayout.MaxHeight(150));
                foreach (var obj in _processedMeshObjects)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
                EditorGUILayout.EndScrollView();
                GUILayout.Space(10);
            }

            // 예외 렌더러 목록 출력
            if (_otherRendererObjects.Count > 0)
            {
                GUILayout.Label("⚠️ 예외 처리된 다른 렌더러 대상", EditorStyles.boldLabel);
                _scrollPositionOther = EditorGUILayout.BeginScrollView(_scrollPositionOther, "box", GUILayout.MaxHeight(150));
                foreach (var obj in _otherRendererObjects)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        // 창이 닫힐 때 자동 복구 로직 (에러 없이 100% 복구됨)
        private void OnDestroy()
        {
            if (_currentState == TransactionState.Staged)
            {
                Debug.LogWarning("[MeshCollider Tool] 최종 확정(Commit)되지 않고 창이 닫혀 자동 취소(Revoke)를 수행합니다.");
                RevokeTransaction(isAutoOnClose: true);
            }
        }

        private void DrawStateHelpBox()
        {
            switch (_currentState)
            {
                case TransactionState.Idle:
                    EditorGUILayout.HelpBox("부모 객체를 설정하고 [확인 (Stage)]을 누르면 작업이 임시 적용됩니다.", UnityEditor.MessageType.Info);
                    break;
                case TransactionState.Staged:
                    EditorGUILayout.HelpBox("⚠️ 현재 변경 사항은 유니티 Undo 스택과 무관한 '가상 상태'입니다.\n다른 곳을 클릭하거나 작업해도 오염되지 않습니다!\n[확정 (Commit)]을 눌러야 실제 씬에 영구 반영됩니다.", UnityEditor.MessageType.Warning);
                    break;
                case TransactionState.Committed:
                    EditorGUILayout.HelpBox("✅ 최종 확정이 완료되었습니다. 씬에 완전히 반영되었습니다.", UnityEditor.MessageType.None);
                    break;
            }
        }

        private void DrawTransactionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // 1. 확인 (Stage)
            EditorGUI.BeginDisabledGroup(_currentState == TransactionState.Staged);
            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
            if (GUILayout.Button("확인 (Stage)", GUILayout.Height(35))) StageTransaction();
            EditorGUI.EndDisabledGroup();

            // 2. 확정 (Commit)
            EditorGUI.BeginDisabledGroup(_currentState != TransactionState.Staged);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("확정 (Commit)", GUILayout.Height(35))) CommitTransaction();

            // 3. 취소 (Revoke)
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("취소 (Revoke)", GUILayout.Height(35))) RevokeTransaction(false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        // ---------------------------------------------------------
        // [1단계] 확인 (Stage) - 글로벌 Undo를 쓰지 않는 가상 트랜잭션
        // ---------------------------------------------------------
        private void StageTransaction()
        {
            if (_targetObject == null) return;

            _tempAddedColliders.Clear();
            _tempHiddenColliders.Clear();
            _processedMeshObjects.Clear();
            _otherRendererObjects.Clear();

            Renderer[] renderers = _targetObject.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer rend in renderers)
            {
                GameObject obj = rend.gameObject;

                if (rend is MeshRenderer)
                {
                    Collider[] colliders = obj.GetComponents<Collider>();

                    if (colliders.Length == 0)
                    {
                        // 1. 추가: Undo 없이 수동 부착 후 추적
                        MeshCollider newCol = obj.AddComponent<MeshCollider>();
                        _tempAddedColliders.Add(newCol);

                        if (!_processedMeshObjects.Contains(obj)) _processedMeshObjects.Add(obj);
                    }
                    else if (colliders.Length > 1)
                    {
                        // 2. 제거 대상: 진짜 지우지 않고 "비활성화 + 인스펙터 숨김" 처리 (완벽한 눈속임)
                        for (int i = 1; i < colliders.Length; i++)
                        {
                            Collider col = colliders[i];
                            col.enabled = false;
                            col.hideFlags = HideFlags.HideInInspector;
                            _tempHiddenColliders.Add(col);
                        }
                        if (!_processedMeshObjects.Contains(obj)) _processedMeshObjects.Add(obj);
                    }
                }
                else
                {
                    if (!_otherRendererObjects.Contains(obj)) _otherRendererObjects.Add(obj);
                }
            }

            _currentState = TransactionState.Staged;
            Repaint();
        }

        // ---------------------------------------------------------
        // [2단계] 확정 (Commit) - 임시 상태를 유니티 공식 기록으로 승격시키는 함수
        // ---------------------------------------------------------
        private void CommitTransaction()
        {
            // =========================================================================
            // 1. Undo 스택에 '새로운 작업 폴더' 만들기
            // =========================================================================
            // 이 함수 안에서 일어날 수십, 수백 번의 추가/삭제 작업을 
            // 나중에 단축키(Ctrl+Z) 한 번으로 되돌릴 수 있게 묶어줄 고유 ID를 발급받습니다.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup MeshColliders"); // Ctrl+Z를 누를 때 뜰 이름 지정
            int groupID = Undo.GetCurrentGroup();            // 방금 만든 그룹의 고유 번호 저장

            // =========================================================================
            // 2. 가짜(임시)로 달아뒀던 콜라이더를 '공식 콜라이더'로 교체하기
            // =========================================================================
            foreach (var col in _tempAddedColliders)
            {
                if (col != null)
                {
                    GameObject obj = col.gameObject; // 콜라이더가 붙어있는 원래 게임 오브젝트 기억

                    // [핵심] 왜 굳이 지울까요?
                    // 이 콜라이더는 유저가 '확인(Stage)'을 눌렀을 때 Undo 엔진 몰래 그냥 달아놓은 가짜입니다.
                    // 이걸 그냥 놔두면 유니티는 "내가 달아준 게 아닌데?" 라며 나중에 Ctrl+Z를 해도 지워주지 않습니다.
                    // 그래서 몰래 달아뒀던 걸 완전히 파괴해서 흔적을 없앱니다.
                    DestroyImmediate(col);

                    // 그리고 유니티의 '공식 결재선'인 Undo 시스템을 통해 진짜 콜라이더를 새로 달아줍니다.
                    // 이제 유니티는 "아! 내가 이 오브젝트에 콜라이더를 추가했었지!" 하고 정확히 기억하게 됩니다.
                    Undo.AddComponent<MeshCollider>(obj);
                }
            }

            // =========================================================================
            // 3. 눈속임으로 숨겨만 뒀던 콜라이더를 '공식 사형(삭제)' 집행하기
            // =========================================================================
            foreach (var col in _tempHiddenColliders)
            {
                if (col != null)
                {
                    // [핵심] 왜 지우기 전에 숨김(HideFlags)을 풀까요?
                    // 만약 숨겨진 상태(HideInInspector) 그대로 Undo.Destroy를 해버리면,
                    // 나중에 유저가 실수해서 Ctrl+Z로 이 콜라이더를 살려냈을 때
                    // '기능은 살아났는데 인스펙터 창에서는 영원히 보이지 않는 유령 콜라이더'가 되어버립니다.
                    // 따라서 죽이기 직전에 정상적인 상태(None)로 옷을 갈아입혀 줍니다.
                    col.hideFlags = HideFlags.None;

                    // 유니티 공식 결재선(Undo)을 통해 오브젝트를 완전히 삭제합니다.
                    // 이제 유니티가 이 삭제 기록을 기억하므로, Ctrl+Z 시 다시 살려낼 수 있습니다.
                    Undo.DestroyObjectImmediate(col);
                }
            }

            // =========================================================================
            // 4. 지금까지 한 작업들을 '하나의 폴더'로 압축(지퍼 닫기)
            // =========================================================================
            // 위 2번과 3번에서 발생한 수많은 Undo.Add와 Undo.Destroy 기록들을
            // 아까 1번에서 발급받은 groupID 패킷 하나로 꽉 묶어버립니다. (Atomic Transaction)
            // 이제부터는 100개가 지워졌든 1개가 지워졌든 Ctrl+Z 한 번에 통째로 취소/복구됩니다.
            Undo.CollapseUndoOperations(groupID);

            // =========================================================================
            // 5. 메모리 청소 및 상태 업데이트
            // =========================================================================
            // 가짜 객체들을 추적하던 리스트를 모두 비워 메모리를 비웁니다.
            _tempAddedColliders.Clear();
            _tempHiddenColliders.Clear();

            // 현재 툴의 상태를 '최종 확정 완료'로 변경합니다.
            _currentState = TransactionState.Committed;

            // UI 갱신 및 완료 팝업 띄우기
            Repaint();
            EditorUtility.DisplayDialog("확정 완료", "변경 사항이 확정되었습니다.\n이제부터 유니티 기본 단축키(Ctrl+Z)로도 복구가 가능합니다.", "확인");
        }

        // ---------------------------------------------------------
        // [3단계] 취소 (Revoke) - 어떠한 꼬임도 없이 완벽한 원상 복귀
        // ---------------------------------------------------------
        private void RevokeTransaction(bool isAutoOnClose)
        {
            // 1. 임시로 추가했던 것들은 수동으로 즉시 파괴
            foreach (var col in _tempAddedColliders)
            {
                if (col != null) DestroyImmediate(col);
            }

            // 2. 지우기 위해 숨겨두었던 녀석들은 다시 활성화시키고 보이게 만듦
            foreach (var col in _tempHiddenColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                    col.hideFlags = HideFlags.None;
                }
            }

            _tempAddedColliders.Clear();
            _tempHiddenColliders.Clear();
            _processedMeshObjects.Clear();
            _otherRendererObjects.Clear();
            _currentState = TransactionState.Idle;

            if (!isAutoOnClose)
            {
                Repaint();
                EditorUtility.DisplayDialog("취소 완료", "작업 이전 상태로 100% 안전하게 복구되었습니다.", "확인");
            }
        }
    }
}