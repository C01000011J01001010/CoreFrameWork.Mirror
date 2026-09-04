using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CoreEditor.EditorTools
{
    public abstract class BaseTransactionToolWindow : EditorWindow
    {
        public enum TransactionState
        {
            Idle,
            Staged,     // 씬에 임시 적용(프리뷰)된 상태
            Committed
        }

        public struct ToolErrorData
        {
            public GameObject TargetObject;
            public string Reason;
        }

        protected TransactionState _currentState = TransactionState.Idle;
        protected string _globalErrorMessage = null;

        protected List<GameObject> _pendingObjects = new List<GameObject>();
        protected List<GameObject> _skippedObjects = new List<GameObject>();
        protected List<ToolErrorData> _errorObjects = new List<ToolErrorData>();

        private Vector2 _scrollPosPending;
        private Vector2 _scrollPosSkipped;
        private Vector2 _scrollPosError;

        protected abstract string GetToolName();
        protected abstract void DrawInputArea();

        // 핵심 로직 3단계
        protected abstract void OnAnalyze(); // 확인: 씬에 임시 부착 (프리뷰)
        protected abstract void OnCommit();  // 확정: 원본 프리팹에 저장
        protected abstract void OnCancel();  // 취소: 씬에 부착한 임시 컴포넌트 삭제

        protected virtual void DrawGuidanceArea() { }

        protected virtual void OnGUI()
        {
            DrawHeaderUI();
            DrawInputArea();

            EditorGUILayout.Space(10);
            DrawGuidanceArea();
            EditorGUILayout.Space(5);

            DrawControlButtons();

            EditorGUILayout.Space(15);

            if (_currentState == TransactionState.Staged || _currentState == TransactionState.Committed)
            {
                DrawStatusLists();
            }
        }

        private void DrawHeaderUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"⚙️ {GetToolName()}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.EndHorizontal();
        }

        private void DrawControlButtons()
        {
            if (_currentState == TransactionState.Idle)
            {
                // 1. 확인 버튼
                if (GUILayout.Button("🔍 확인 (미리보기 적용)", GUILayout.Height(35)))
                {
                    ExecuteAnalyze();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();

                // 2. 취소 버튼
                if (GUILayout.Button("↩️ 취소 (원상복구)", GUILayout.Height(30)))
                {
                    ExecuteCancel();
                }

                bool canCommit = _errorObjects.Count == 0 && string.IsNullOrEmpty(_globalErrorMessage) && _pendingObjects.Count > 0 && _currentState == TransactionState.Staged;

                // 3. 확정 버튼
                EditorGUI.BeginDisabledGroup(!canCommit);
                if (GUILayout.Button("💾 확정 (프리팹 원본 저장)", GUILayout.Height(30)))
                {
                    ExecuteCommit();
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
            }
        }

        private void ExecuteAnalyze()
        {
            ResetTransactionData();
            OnAnalyze();
            _currentState = TransactionState.Staged;
            Repaint();
        }

        private void ExecuteCommit()
        {
            OnCommit();
            _currentState = TransactionState.Committed;
            Repaint();
        }

        private void ExecuteCancel()
        {
            OnCancel(); // 자식 클래스에서 씬 원상복구 로직 실행
            ResetTransactionData();
            _currentState = TransactionState.Idle;
            Repaint();
        }

        protected void ResetTransaction()
        {
            ExecuteCancel(); // 범용적인 초기화도 Cancel을 통하도록 변경
        }

        protected void ResetTransactionData()
        {
            _globalErrorMessage = null;
            _pendingObjects.Clear();
            _skippedObjects.Clear();
            _errorObjects.Clear();
        }

        protected virtual void DrawStatusLists()
        {
            if (!string.IsNullOrEmpty(_globalErrorMessage))
            {
                EditorGUILayout.HelpBox($"🚨 작업 차단됨: {_globalErrorMessage}", MessageType.Error);
                return;
            }

            if (_errorObjects.Count > 0)
            {
                EditorGUILayout.LabelField($"🔴 작업 차단 및 에러 객체 ({_errorObjects.Count})", EditorStyles.boldLabel);
                _scrollPosError = EditorGUILayout.BeginScrollView(_scrollPosError, GUILayout.Height(120));
                foreach (var err in _errorObjects)
                {
                    GUI.color = new Color(1f, 0.7f, 0.7f);
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.ObjectField(err.TargetObject, typeof(GameObject), true, GUILayout.Width(200));
                    GUILayout.Label($"➔ {err.Reason}", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
                GUI.color = Color.white;
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.LabelField($"🟢 컴포넌트 임시 적용됨 (확정 대기중) ({_pendingObjects.Count})", EditorStyles.boldLabel);
            _scrollPosPending = EditorGUILayout.BeginScrollView(_scrollPosPending, GUILayout.Height(150));
            foreach (var obj in _pendingObjects)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField($"⚪ 이미 모든 컴포넌트가 세팅되어 제외됨 ({_skippedObjects.Count})", EditorStyles.boldLabel);
            _scrollPosSkipped = EditorGUILayout.BeginScrollView(_scrollPosSkipped, GUILayout.Height(100));
            foreach (var obj in _skippedObjects)
            {
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            GUI.color = Color.white;
        }
    }
}