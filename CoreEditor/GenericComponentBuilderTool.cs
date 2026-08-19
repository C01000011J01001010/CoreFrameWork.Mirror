using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomTools.Editor
{
    public class GenericComponentBuilderTool : EditorWindow
    {
        private enum TransactionState { Idle, Staged, Committed }

        [Serializable]
        private class ComponentTarget
        {
            public MonoScript ScriptAsset;
            public string TypeName = "";
            public Type ComponentType;
        }

        private GameObject _targetObject;
        private TransactionState _transactionState = TransactionState.Idle;

        private List<ComponentTarget> _conditionList = new List<ComponentTarget> { new ComponentTarget { TypeName = "MeshRenderer", ComponentType = typeof(MeshRenderer) } };
        private List<ComponentTarget> _targetList = new List<ComponentTarget> { new ComponentTarget { TypeName = "MeshCollider", ComponentType = typeof(MeshCollider) } };

        private List<Component> _tempAddedComponents = new List<Component>();
        private List<Component> _tempHiddenComponents = new List<Component>();
        private List<GameObject> _processedObjects = new List<GameObject>();

        private Vector2 _scrollCondition;
        private Vector2 _scrollTarget;
        private Vector2 _scrollProcessed;

        [MenuItem("Tools/Generic Component Builder Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<GenericComponentBuilderTool>("Component Builder");
            window.minSize = new Vector2(550, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("⚙️ 범용 하이브리드 컴포넌트 빌더 (버그 픽스 완료)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawStateHelpBox();
            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(_transactionState == TransactionState.Staged);
            _targetObject = (GameObject)EditorGUILayout.ObjectField("Target Parent", _targetObject, typeof(GameObject), true);
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            EditorVerticalLayout("🔍 조건 컴포넌트", _conditionList, ref _scrollCondition);
            GUILayout.FlexibleSpace();
            EditorVerticalLayout("🔨 타겟 컴포넌트", _targetList, ref _scrollTarget);

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(15);
            DrawTransactionButtons();
            GUILayout.Space(15);

            if (_processedObjects.Count > 0)
            {
                string prefix = _transactionState == TransactionState.Staged ? "[임시 대기]" : "[최종 확정]";
                GUILayout.Label($"{prefix} 컴포넌트 조작 대상 ({_processedObjects.Count}개)", EditorStyles.boldLabel);
                _scrollProcessed = EditorGUILayout.BeginScrollView(_scrollProcessed, "box", GUILayout.MaxHeight(180));
                foreach (var obj in _processedObjects)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void EditorVerticalLayout(string title, List<ComponentTarget> list, ref Vector2 scroll)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.48f));
            DrawComponentListUI(list, ref scroll, title);
            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            if (_transactionState == TransactionState.Staged) RevokeTransaction(true);
        }

        private void DrawComponentListUI(List<ComponentTarget> list, ref Vector2 scroll, string title)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            if (GUILayout.Button("항목 추가 (+)", GUILayout.Height(20)))
            {
                list.Add(new ComponentTarget());
            }

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(130));
            int indexToRemove = -1;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                int currentIndex = i;

                EditorGUILayout.BeginHorizontal();

                MonoScript droppedScript = (MonoScript)EditorGUILayout.ObjectField(item.ScriptAsset, typeof(MonoScript), false, GUILayout.Width(70));

                if (droppedScript != item.ScriptAsset)
                {
                    if (droppedScript != null)
                    {
                        Type t = droppedScript.GetClass();

                        if (t == null)
                        {
                            EditorUtility.DisplayDialog("스크립트 인식 불가", "파일명과 코드 내부의 클래스 이름이 일치하는지 확인해주세요.", "확인");
                            droppedScript = null;
                        }
                        else if (!typeof(Component).IsAssignableFrom(t))
                        {
                            EditorUtility.DisplayDialog("타입 오류", "오직 Component를 상속받은 스크립트만 등록 가능합니다.", "확인");
                            droppedScript = null;
                        }
                        else if (t.IsAbstract)
                        {
                            EditorUtility.DisplayDialog("인스펙터 등록 불가 타입", $"'{t.Name}' 클래스는 추상(Abstract) 클래스이므로 오브젝트에 부착할 수 없습니다.", "확인");
                            droppedScript = null;
                        }
                        else
                        {
                            if (CheckIsDuplicate(t.Name, list, currentIndex)) droppedScript = null;
                            else
                            {
                                item.TypeName = t.Name;
                                item.ComponentType = t;
                            }
                        }
                    }
                    else
                    {
                        item.TypeName = "";
                        item.ComponentType = null;
                    }
                    item.ScriptAsset = droppedScript;
                }

                string btnText = string.IsNullOrEmpty(item.TypeName) ? "(컴포넌트 선택...)" : item.TypeName;
                if (GUILayout.Button(btnText, EditorStyles.popup))
                {
                    var dropdown = new ComponentSearchDropdown(new AdvancedDropdownState());
                    dropdown.OnItemSelected += (selectedType) =>
                    {
                        if (!CheckIsDuplicate(selectedType.Name, list, currentIndex))
                        {
                            item.TypeName = selectedType.Name;
                            item.ComponentType = selectedType;
                            item.ScriptAsset = null;
                            Repaint();
                        }
                    };

                    dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
                }

                if (GUILayout.Button("X", GUILayout.Width(25))) indexToRemove = i;

                EditorGUILayout.EndHorizontal();
            }

            if (indexToRemove != -1) list.RemoveAt(indexToRemove);
            EditorGUILayout.EndScrollView();
        }

        private bool CheckIsDuplicate(string nameToCheck, List<ComponentTarget> list, int currentIndex)
        {
            if (string.IsNullOrEmpty(nameToCheck)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (i != currentIndex && list[i].TypeName == nameToCheck)
                {
                    EditorUtility.DisplayDialog("중복 차단", $"이미 리스트에 '{nameToCheck}' 컴포넌트가 있습니다.", "확인");
                    return true;
                }
            }
            return false;
        }

        private void StageTransaction()
        {
            if (_targetObject == null) return;

            foreach (var cond in _conditionList)
            {
                if (cond.ComponentType == null) continue;
                if (_targetList.Any(t => t.ComponentType == cond.ComponentType))
                {
                    EditorUtility.DisplayDialog("규칙 모순 발생", "조건과 타겟에 동일한 컴포넌트가 존재할 수 없습니다.", "확인");
                    return;
                }
            }

            List<Type> conditionTypes = _conditionList.Select(c => c.ComponentType).Where(t => t != null).ToList();
            List<Type> targetTypes = _targetList.Select(c => c.ComponentType).Where(t => t != null).ToList();

            foreach (var tType in targetTypes)
            {
                if (tType.IsAbstract)
                {
                    EditorUtility.DisplayDialog("타겟팅 무효", $"'{tType.Name}'은 부모/추상형 타입입니다.\n실제 오브젝트에 장착 가능한 파생 컴포넌트를 선택해주세요.", "확인");
                    return;
                }
            }

            if (conditionTypes.Count == 0 || targetTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "유효한 컴포넌트를 최소 1개 이상 세팅해주세요.", "확인");
                return;
            }

            _tempAddedComponents.Clear();
            _tempHiddenComponents.Clear();
            _processedObjects.Clear();

            Transform[] allChildren = _targetObject.GetComponentsInChildren<Transform>(true);

            foreach (var child in allChildren)
            {
                GameObject obj = child.gameObject;
                if (obj == _targetObject) continue;

                bool isMatch = true;
                foreach (Type condType in conditionTypes)
                {
                    if (obj.GetComponent(condType) == null) { isMatch = false; break; }
                }
                if (!isMatch) continue;

                foreach (Type targetType in targetTypes)
                {
                    Component[] components = obj.GetComponents(targetType);

                    if (components.Length == 0)
                    {
                        Component newComp = obj.AddComponent(targetType);

                        // ✨ [버그 픽스 및 예외 처리] 부착 실패 시 롤백 및 알림창 발생
                        if (newComp != null)
                        {
                            _tempAddedComponents.Add(newComp);
                            if (!_processedObjects.Contains(obj)) _processedObjects.Add(obj);
                        }
                        else
                        {
                            // 1. 지금까지 처리하던 임시 가상 데이터를 모두 깔끔하게 원상복구 시킵니다.
                            RevokeTransaction(isAutoOnClose: true);

                            // 2. 유저에게 정확한 실패 원인과 대상을 고지합니다.
                            EditorUtility.DisplayDialog(
                                "컴포넌트 부착 실패",
                                $"'{obj.name}' 오브젝트에 '{targetType.Name}' 컴포넌트를 부착할 수 없습니다.\n\n" +
                                $"해당 컴포넌트는 단독으로 부착할 수 없거나 유니티 내부 규칙(RequireComponent 충돌 등)에 의해 제한된 타입입니다.",
                                "확인"
                            );

                            // 3. 연산을 즉시 중단합니다.
                            return;
                        }
                    }
                    else if (components.Length > 1)
                    {
                        for (int i = 1; i < components.Length; i++)
                        {
                            Component comp = components[i];
                            if (comp == null) continue;

                            if (comp is Behaviour b) b.enabled = false;
                            else if (comp is Renderer r) r.enabled = false;
                            else if (comp is Collider c) c.enabled = false;

                            comp.hideFlags = HideFlags.HideInInspector;
                            _tempHiddenComponents.Add(comp);
                        }
                        if (!_processedObjects.Contains(obj)) _processedObjects.Add(obj);
                    }
                }
            }

            if (_tempAddedComponents.Count == 0 && _tempHiddenComponents.Count == 0)
            {
                EditorUtility.DisplayDialog("결과 없음", "지정한 조건과 매칭되거나 정제할 타겟 오브젝트가 자식 노드에 존재하지 않습니다.", "확인");
                return;
            }

            _transactionState = TransactionState.Staged;
            Repaint();
        }

        private void CommitTransaction()
        {
            Undo.IncrementCurrentGroup();
            int groupID = Undo.GetCurrentGroup();

            foreach (var comp in _tempAddedComponents)
            {
                if (comp != null)
                {
                    GameObject obj = comp.gameObject;
                    Type t = comp.GetType();
                    DestroyImmediate(comp);

                    if (t != null && !t.IsAbstract)
                    {
                        Undo.AddComponent(obj, t);
                    }
                }
            }
            foreach (var comp in _tempHiddenComponents)
            {
                if (comp != null)
                {
                    comp.hideFlags = HideFlags.None;
                    Undo.DestroyObjectImmediate(comp);
                }
            }
            Undo.CollapseUndoOperations(groupID);
            _tempAddedComponents.Clear();
            _tempHiddenComponents.Clear();
            _transactionState = TransactionState.Committed;
            Repaint();
        }

        private void RevokeTransaction(bool isAutoOnClose)
        {
            foreach (var comp in _tempAddedComponents) { if (comp != null) DestroyImmediate(comp); }
            foreach (var comp in _tempHiddenComponents)
            {
                if (comp != null)
                {
                    comp.hideFlags = HideFlags.None;
                    if (comp is Behaviour b) b.enabled = true;
                    else if (comp is Renderer r) r.enabled = true;
                    else if (comp is Collider c) c.enabled = true;
                }
            }
            _tempAddedComponents.Clear();
            _tempHiddenComponents.Clear();
            _processedObjects.Clear();
            _transactionState = TransactionState.Idle;
            if (!isAutoOnClose) Repaint();
        }

        private void DrawStateHelpBox()
        {
            switch (_transactionState)
            {
                case TransactionState.Idle:
                    EditorGUILayout.HelpBox("드롭다운 버튼을 클릭해 컴포넌트를 검색하거나 스크립트를 드래그하세요.\n(부모/추상 클래스 타입인 Collider나 Renderer 등은 가드 장치에 의해 필터링됩니다.)", UnityEditor.MessageType.Info);
                    break;
                case TransactionState.Staged:
                    EditorGUILayout.HelpBox("⚠️ 가상 그림자 상태입니다. [확정]을 누르면 실제 씬에 영구 반영됩니다.", UnityEditor.MessageType.Warning);
                    break;
                case TransactionState.Committed:
                    EditorGUILayout.HelpBox("✅ 커밋 확정 완료.", UnityEditor.MessageType.None);
                    break;
            }
        }

        private void DrawTransactionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_transactionState == TransactionState.Staged);
            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
            if (GUILayout.Button("확인 (Stage)", GUILayout.Height(35))) StageTransaction();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_transactionState != TransactionState.Staged);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("확정 (Commit)", GUILayout.Height(35))) CommitTransaction();

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("취소 (Revoke)", GUILayout.Height(35))) RevokeTransaction(false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

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