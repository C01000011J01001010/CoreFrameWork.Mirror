#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CoreEngine.EventBus;

public class EventBusDebuggerWindow : EditorWindow
{
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Event Bus Debugger")]
    public static void ShowWindow()
    {
        GetWindow<EventBusDebuggerWindow>("Event Bus Debugger");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Event Bus 글로벌 제어", EditorStyles.boldLabel);

        // 1. 마스터 스위치 (버튼 하나로 모든 로그 켜고 끄기)
        EventBusRegistry.MasterDebugLog = EditorGUILayout.Toggle("전체 Event 로그 활성화", EventBusRegistry.MasterDebugLog);

        if (GUILayout.Button("모든 EventBus 구독 강제 초기화 (Clear All)"))
        {
            if (EditorUtility.DisplayDialog("경거망동 금지", "정말 모든 이벤트를 초기화하시겠습니까?", "예", "아니오"))
            {
                foreach (var bus in EventBusRegistry.ActiveBuses) bus.ClearBus();
            }
        }

        GUILayout.Space(15);
        GUILayout.Label($"활성화된 EventBus 리스트 ({EventBusRegistry.ActiveBuses.Count}개)", EditorStyles.boldLabel);

        // 2. 런타임에 Publish가 한 번이라도 일어난 버스들을 리스트로 출력
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);

        if (EventBusRegistry.ActiveBuses.Count == 0)
        {
            GUILayout.Label("현재 활성화된(메모리에 로드된) EventBus가 없습니다.\n게임 플레이 중에 이벤트가 최소 1회 발생해야 등록됩니다.", EditorStyles.wordWrappedLabel);
        }
        else
        {
            foreach (var bus in EventBusRegistry.ActiveBuses)
            {
                EditorGUILayout.BeginHorizontal();

                // 이벤트 구조체 이름과 현재 대기 중인 리스너 수 출력
                GUILayout.Label($"[{bus.EventTypeName}] (리스너: {bus.SubscriberCount}명)", GUILayout.Width(250));

                // 개별 On/Off 토글 버튼
                GUI.enabled = !EventBusRegistry.MasterDebugLog; // 마스터 스위치가 켜져있으면 개별 스위치는 비활성화
                bus.DebugLogEnabled = EditorGUILayout.Toggle(bus.DebugLogEnabled, GUILayout.Width(30));
                GUI.enabled = true;

                // 개별 Clear 버튼
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    bus.ClearBus();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }

        GUILayout.EndScrollView();

        // 에디터 윈도우가 실시간으로 갱신되도록 유도 (수신자 수 변화 등을 반영)
        if (Application.isPlaying) Repaint();
    }
}
#endif