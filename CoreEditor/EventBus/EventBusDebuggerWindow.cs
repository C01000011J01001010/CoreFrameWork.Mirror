#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CoreEngine.EventBus;
using System.Collections.Generic;
using System.Linq;

namespace CoreEditor.EditorTools
{
    public class EventBusDebuggerWindow : EditorWindow
    {
        private enum SortMode { CreationOrder, Alphabetical }
        private SortMode _sortMode = SortMode.CreationOrder;

        private Vector2 _scrollPosition;

        // 이벤트를 클릭했을 때 하위 목록을 펼쳐서 보여줄 토글 상태 저장
        private HashSet<string> _expandedBuses = new();

        // 매 프레임 Repaint 방지를 위한 최적화 타이머
        private double _lastRepaintTime;
        private const double REPAINT_INTERVAL = 0.5; // 0.5초마다 UI 갱신

        [MenuItem(Constants.ToolRoot+"Event Bus Debugger")]
        //[MenuItem("Tools/Event Bus Debugger")]
        public static void ShowWindow()
        {
            GetWindow<EventBusDebuggerWindow>("Event Bus Debugger");
        }

        private void OnEnable()
        {
            // 에디터 업데이트 틱에 구독하여 최적화된 갱신 루프 구성
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // 플레이 중일 때만 지정된 간격(0.5초)으로 갱신하여 프레임 드랍(렉) 방지
            if (Application.isPlaying && EditorApplication.timeSinceStartup - _lastRepaintTime > REPAINT_INTERVAL)
            {
                Repaint();
                _lastRepaintTime = EditorApplication.timeSinceStartup;
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Event Bus 글로벌 제어", EditorStyles.boldLabel);

            EventBusRegistry.MasterDebugLog = EditorGUILayout.Toggle("전체 Event 로그 활성화", EventBusRegistry.MasterDebugLog);

            if (GUILayout.Button("모든 EventBus 구독 강제 초기화 (Clear All)"))
            {
                if (EditorUtility.DisplayDialog("경고", "정말 모든 이벤트를 초기화하시겠습니까?", "예", "아니오"))
                {
                    foreach (var bus in EventBusRegistry.ActiveBuses) bus.ClearBus();
                }
            }

            GUILayout.Space(10);

            // 정렬 모드 선택 드롭다운
            _sortMode = (SortMode)EditorGUILayout.EnumPopup("이벤트 정렬 방식", _sortMode);

            GUILayout.Space(15);
            GUILayout.Label($"활성화된 EventBus 리스트 ({EventBusRegistry.ActiveBuses.Count}개)", EditorStyles.boldLabel);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);

            if (EventBusRegistry.ActiveBuses.Count == 0)
            {
                GUILayout.Label("현재 활성화된(메모리에 로드된) EventBus가 없습니다.\n게임 플레이 중에 이벤트가 최소 1회 발생해야 등록됩니다.", EditorStyles.wordWrappedLabel);
            }
            else
            {
                // 원본 리스트 복사 후 정렬 로직 적용
                var displayList = new List<IEventBusControl>(EventBusRegistry.ActiveBuses);

                if (_sortMode == SortMode.Alphabetical)
                {
                    displayList = displayList.OrderBy(b => b.EventTypeName).ToList();
                }

                char lastChar = '\0';

                foreach (var bus in displayList)
                {
                    // 이름 정렬 시, 첫 글자가 바뀌는 시점마다 구분 박스 렌더링
                    if (_sortMode == SortMode.Alphabetical)
                    {
                        char currentChar = bus.EventTypeName.ToUpper()[0];
                        if (currentChar != lastChar)
                        {
                            lastChar = currentChar;
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal("box");
                            GUILayout.Label($" ❖ {currentChar}", EditorStyles.boldLabel);
                            GUILayout.EndHorizontal();
                        }
                    }

                    DrawBusItem(bus);
                }
            }

            GUILayout.EndScrollView();

            if (Application.isPlaying) Repaint();
        }

        private void DrawBusItem(IEventBusControl bus)
        {
            EditorGUILayout.BeginVertical("helpBox");
            bool isExpanded = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                // 이벤트 클릭식 Foldout 구현
                isExpanded = _expandedBuses.Contains(bus.EventTypeName);
                string foldoutStr = isExpanded ? "▼" : "▶";

                // 이름을 버튼으로 만들어 클릭 시 확장/축소 토글
                if (GUILayout.Button($"{foldoutStr} [{bus.EventTypeName}] (리스너: {bus.SubscriberCount}명)", EditorStyles.label, GUILayout.Width(250)))
                {
                    if (isExpanded) _expandedBuses.Remove(bus.EventTypeName);
                    else _expandedBuses.Add(bus.EventTypeName);
                }

                GUI.enabled = !EventBusRegistry.MasterDebugLog;
                bus.DebugLogEnabled = EditorGUILayout.Toggle(bus.DebugLogEnabled, GUILayout.Width(30));
                GUI.enabled = true;

                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    bus.ClearBus();
                }
            }

            // 확장 시 하위 리스너 목록 출력
            if (isExpanded)
            {
                var subscribers = bus.GetSubscribers();
                if (subscribers == null || subscribers.Length == 0)
                {
                    GUILayout.Label("    └ 대기 중인 리스너가 없습니다.", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var sub in subscribers)
                    {
                        if (sub == null) continue;

                        // 델리게이트에 바인딩된 원본 객체(Target) 추출
                        UnityEngine.Object targetObj = sub.Target as UnityEngine.Object;

                        // 유니티의 가짜 널(Fake Null)을 활용한 좀비 리스너 검출
                        // C# 메모리에는 존재하지만(isUnityObject == true), 씬에서는 파괴된(targetObj == null) 객체 색출
                        bool isUnityObject = sub.Target is UnityEngine.Object;
                        bool isZombie = isUnityObject && targetObj == null;

                        string targetName = isZombie
                            ? "[메모리 누수 의심] 파괴된 객체"
                            : (targetObj != null ? targetObj.name : (sub.Target?.GetType().Name ?? "Static Method"));

                        string methodName = sub.Method.Name;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(25);

                            // 좀비 리스너일 경우 강렬한 붉은색으로 스타일링
                            GUIStyle pingButtonStyle = new GUIStyle(EditorStyles.linkLabel);
                            if (isZombie)
                            {
                                pingButtonStyle.normal.textColor = Color.red;
                                pingButtonStyle.hover.textColor = new Color(1f, 0.4f, 0.4f);
                            }

                            // Ping 버튼 기능 (인스펙터 전환 없이 하이라키/프로젝트 하이라이트)
                            if (GUILayout.Button($"🔍 {targetName} ➔ {methodName}()", EditorStyles.linkLabel))
                            {
                                if (targetObj != null)
                                {
                                    EditorGUIUtility.PingObject(targetObj);
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }
    }
}

#endif