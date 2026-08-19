using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomTools.Editor
{
    public class TagComponentSetupTool : EditorWindow
    {
        private enum TransactionState
        {
            Idle,       // 대기 상태
            Staged,     // 확인(Stage) 적용 후 확정 대기 상태
            Committed   // 최종 확정 완료 상태
        }

        // ==========================================
        // 🎯 1. 타겟 세팅 데이터 
        // ==========================================
        private string _selectedTag = "Untagged";

        private MonoScript _droppedScript;
        private Type _targetComponentType;
        private string _targetTypeName = "";

        private TransactionState _currentState = TransactionState.Idle;

        // 🌟 2. 그림자 상태(Shadow State) 데이터
        private List<Component> _tempAddedComponents = new List<Component>();

        // 📊 3. UI 갱신용 리스트 (3분할)
        private List<GameObject> _processedObjects = new List<GameObject>();
        private List<GameObject> _ignoredObjects = new List<GameObject>();
        private List<GameObject> _errorObjects = new List<GameObject>();

        // ✨ 개선점: 내부 리스트별 스크롤 변수들을 모두 제거하고, 창 전체를 관장하는 메인 스크롤 변수 하나만 사용합니다.
        private Vector2 _mainWindowScrollPosition;

        [MenuItem("Tools/Tag Component Setup Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<TagComponentSetupTool>("Tag Component Setup");
            window.minSize = new Vector2(450, 650);
            window.Show();
        }

        private void OnGUI()
        {
            // 🌐 창 전체를 덮는 메인 스크롤 뷰 시작
            _mainWindowScrollPosition = EditorGUILayout.BeginScrollView(_mainWindowScrollPosition);

            GUILayout.Space(10);
            GUILayout.Label("🏷️ Tag-Based Component Setup Tool (V3.2)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawStateHelpBox();

            GUILayout.Space(10);

            // ==========================================
            // 🎛️ 설정 영역
            // ==========================================
            EditorGUI.BeginDisabledGroup(_currentState == TransactionState.Staged);

            // 1. 태그 선택
            _selectedTag = EditorGUILayout.TagField("Target Tag", _selectedTag);
            GUILayout.Space(5);

            // 2. 컴포넌트 선택
            DrawComponentSelectionUI();

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(15);
            DrawTransactionButtons();
            GUILayout.Space(15);

            // ==========================================
            // 📋 처리 결과 리스트 UI
            // ==========================================
            DrawResultListsUI();

            // 🌐 메인 스크롤 뷰 종료
            EditorGUILayout.EndScrollView();
        }

        private void DrawComponentSelectionUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Component to Add", GUILayout.Width(EditorGUIUtility.labelWidth - 2));

            MonoScript newScript = (MonoScript)EditorGUILayout.ObjectField(_droppedScript, typeof(MonoScript), false, GUILayout.Width(70));
            if (newScript != _droppedScript)
            {
                _droppedScript = newScript;
                if (_droppedScript != null)
                {
                    Type t = _droppedScript.GetClass();
                    if (t == null)
                    {
                        EditorUtility.DisplayDialog("스크립트 인식 불가", "파일명과 코드 내부의 클래스 이름이 일치하는지 확인해주세요.", "확인");
                        _droppedScript = null;
                    }
                    else if (!typeof(Component).IsAssignableFrom(t))
                    {
                        EditorUtility.DisplayDialog("타입 오류", "오직 Component를 상속받은 스크립트만 등록 가능합니다.", "확인");
                        _droppedScript = null;
                    }
                    else if (t.IsAbstract)
                    {
                        EditorUtility.DisplayDialog("부착 불가 타입", $"'{t.Name}' 클래스는 추상(Abstract) 클래스이므로 오브젝트에 부착할 수 없습니다.", "확인");
                        _droppedScript = null;
                    }
                    else
                    {
                        _targetComponentType = t;
                        _targetTypeName = t.Name;
                    }
                }
                else
                {
                    _targetComponentType = null;
                    _targetTypeName = "";
                }
            }

            string btnText = string.IsNullOrEmpty(_targetTypeName) ? "(컴포넌트 검색 및 선택...)" : _targetTypeName;
            if (GUILayout.Button(btnText, EditorStyles.popup))
            {
                var dropdown = new ComponentSearchDropdown(new AdvancedDropdownState());
                dropdown.OnItemSelected += (selectedType) =>
                {
                    _targetComponentType = selectedType;
                    _targetTypeName = selectedType.Name;
                    _droppedScript = null;
                    Repaint();
                };
                dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawResultListsUI()
        {
            // 1. 오류 대상 (2개 이상 중복)
            if (_errorObjects.Count > 0)
            {
                GUI.contentColor = new Color(1.0f, 0.4f, 0.4f);
                GUILayout.Label($"❌ 오류: 중복 컴포넌트 발견 ({_errorObjects.Count}개)", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                // ✨ 개선점: ScrollView 대신 Vertical("box")를 사용하여 아이템 개수에 정비례하여 길이가 자연스럽게 늘어남
                EditorGUILayout.BeginVertical("box");
                foreach (var obj in _errorObjects)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // 2. 작업 대상 (성공)
            if (_processedObjects.Count > 0)
            {
                string prefix = _currentState == TransactionState.Staged ? "[임시 대기]" : "[최종 확정]";
                GUI.contentColor = new Color(0.6f, 1.0f, 0.6f);
                GUILayout.Label($"{prefix} 컴포넌트 부착 대상 ({_processedObjects.Count}개)", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.BeginVertical("box");
                foreach (var obj in _processedObjects)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // 3. 작업 무시 (정상적으로 1개 존재)
            if (_ignoredObjects.Count > 0)
            {
                GUI.contentColor = new Color(1.0f, 0.8f, 0.4f);
                GUILayout.Label($"⚠️ 건너뜀: 이미 컴포넌트가 존재함 ({_ignoredObjects.Count}개)", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.BeginVertical("box");
                foreach (var obj in _ignoredObjects)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void OnDestroy()
        {
            if (_currentState == TransactionState.Staged) RevokeTransaction(isAutoOnClose: true);
        }

        private void DrawStateHelpBox()
        {
            switch (_currentState)
            {
                case TransactionState.Idle:
                    EditorGUILayout.HelpBox("태그를 선택하고 컴포넌트를 지정하세요.\n(이미 2개 이상 중복 부착된 객체는 오류 리스트로 분류됩니다.)", UnityEditor.MessageType.Info);
                    break;
                case TransactionState.Staged:
                    EditorGUILayout.HelpBox("⚠️ 현재 변경 사항은 유니티 Undo 스택과 무관한 '가상 상태'입니다.\n결과를 확인하고 [확정 (Commit)]을 눌러 씬에 반영하세요.", UnityEditor.MessageType.Warning);
                    break;
                case TransactionState.Committed:
                    EditorGUILayout.HelpBox("✅ 최종 확정이 완료되었습니다. 유니티 단축키(Ctrl+Z)로 복구 가능합니다.", UnityEditor.MessageType.None);
                    break;
            }
        }

        private void DrawTransactionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(_currentState == TransactionState.Staged || _targetComponentType == null);
            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
            if (GUILayout.Button("확인 (Stage)", GUILayout.Height(35))) StageTransaction();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_currentState != TransactionState.Staged);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("확정 (Commit)", GUILayout.Height(35))) CommitTransaction();

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("취소 (Revoke)", GUILayout.Height(35))) RevokeTransaction(false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        // ---------------------------------------------------------
        // 트랜잭션 로직
        // ---------------------------------------------------------
        private void StageTransaction()
        {
            if (_targetComponentType == null) return;

            _tempAddedComponents.Clear();
            _processedObjects.Clear();
            _ignoredObjects.Clear();
            _errorObjects.Clear();

            GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(_selectedTag);

            if (targetObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", $"'{_selectedTag}' 태그를 가진 활성화된 게임 오브젝트가 씬에 없습니다.", "확인");
                return;
            }

            bool hasFatalError = false;

            foreach (GameObject obj in targetObjects)
            {
                Component[] existingComponents = obj.GetComponents(_targetComponentType);
                int compCount = existingComponents.Length;

                if (compCount >= 2)
                {
                    _errorObjects.Add(obj);
                }
                else if (compCount == 1)
                {
                    _ignoredObjects.Add(obj);
                }
                else
                {
                    Component newComp = null;
                    try
                    {
                        newComp = obj.AddComponent(_targetComponentType);
                    }
                    catch { newComp = null; }

                    if (newComp == null)
                    {
                        hasFatalError = true;
                        break;
                    }

                    _tempAddedComponents.Add(newComp);
                    _processedObjects.Add(obj);
                }
            }

            if (hasFatalError)
            {
                RevokeTransaction(isAutoOnClose: true);
                EditorUtility.DisplayDialog("부착 거부됨", $"엔진 내부 규칙(RequireComponent 충돌 등)에 의해 '{_targetTypeName}'을(를) 추가할 수 없는 오브젝트가 발견되어 작업을 취소했습니다.", "확인");
                return;
            }

            if (_tempAddedComponents.Count == 0 && _errorObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("결과", "모든 대상 객체에 이미 해당 컴포넌트가 1개씩 존재하여 추가 작업이 진행되지 않았습니다.", "확인");
                return;
            }

            _currentState = TransactionState.Staged;
            Repaint();
        }

        private void CommitTransaction()
        {
            if (_targetComponentType == null) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Add {_targetComponentType.Name} by Tag");
            int groupID = Undo.GetCurrentGroup();

            foreach (var comp in _tempAddedComponents)
            {
                if (comp != null)
                {
                    GameObject obj = comp.gameObject;
                    DestroyImmediate(comp);
                    Undo.AddComponent(obj, _targetComponentType);
                }
            }

            Undo.CollapseUndoOperations(groupID);

            _tempAddedComponents.Clear();
            _currentState = TransactionState.Committed;
            Repaint();
        }

        private void RevokeTransaction(bool isAutoOnClose)
        {
            foreach (var comp in _tempAddedComponents)
            {
                if (comp != null) DestroyImmediate(comp);
            }

            _tempAddedComponents.Clear();
            _processedObjects.Clear();
            _ignoredObjects.Clear();
            _errorObjects.Clear();

            _currentState = TransactionState.Idle;
            if (!isAutoOnClose) Repaint();
        }

        // =========================================================
        // 🔍 AdvancedDropdown 클래스 
        // =========================================================
        private class ComponentSearchDropdown : AdvancedDropdown
        {
            public Action<Type> OnItemSelected;

            private class ComponentDropdownItem : AdvancedDropdownItem
            {
                public Type ComponentType { get; }
                public ComponentDropdownItem(string name, Type type) : base(name)
                {
                    ComponentType = type;
                }
            }

            public ComponentSearchDropdown(AdvancedDropdownState state) : base(state)
            {
                minimumSize = new Vector2(250, 300);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("컴포넌트 선택");

                var componentTypes = TypeCache.GetTypesDerivedFrom<Component>()
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                    .OrderBy(t => t.Name);

                foreach (Type type in componentTypes)
                {
                    if (type.Name.StartsWith("Skeleton") || type.Namespace == "UnityEditor") continue;

                    string[] namespaces = (type.Namespace ?? "Scripts (Custom)").Split('.');

                    AdvancedDropdownItem currentGroup = root;
                    foreach (string ns in namespaces)
                    {
                        var foundGroup = currentGroup.children.FirstOrDefault(c => c.name == ns);
                        if (foundGroup == null)
                        {
                            foundGroup = new AdvancedDropdownItem(ns);
                            currentGroup.AddChild(foundGroup);
                        }
                        currentGroup = foundGroup;
                    }

                    currentGroup.AddChild(new ComponentDropdownItem(type.Name, type));
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is ComponentDropdownItem compItem)
                {
                    OnItemSelected?.Invoke(compItem.ComponentType);
                }
            }
        }
    }
}