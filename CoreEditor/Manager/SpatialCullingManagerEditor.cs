using UnityEditor;
using UnityEngine;
using CoreEngine.Manager.Culling; // 매니저가 있는 네임스페이스

[CustomEditor(typeof(SpatialCullingManager))]
public class SpatialCullingManagerEditor : Editor
{
    private SerializedProperty cullingAxisProp;
    private SerializedProperty cellSizeProp;
    private SerializedProperty aProp;
    private SerializedProperty bProp;
    private SerializedProperty showDebugGridProp;
    private SerializedProperty IsDrawSelectedProp;
    private SerializedProperty debugPlaneHeightProp;

    private void OnEnable()
    {
        // 변수들을 캐싱합니다. (변수명이 실제 스크립트와 동일해야 함)
        cullingAxisProp = serializedObject.FindProperty("cullingAxis");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        aProp = serializedObject.FindProperty("a");
        bProp = serializedObject.FindProperty("b");

        showDebugGridProp = serializedObject.FindProperty("showDebugGrid");
        IsDrawSelectedProp = serializedObject.FindProperty("IsDrawSelected");
        debugPlaneHeightProp = serializedObject.FindProperty("debugPlaneHeight");
    }

    public override void OnInspectorGUI()
    {
        // 최신 데이터를 가져옵니다.
        serializedObject.Update();

        // 1. Grid Settings 영역
        EditorGUILayout.LabelField("🗺️ Grid Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cullingAxisProp);
        EditorGUILayout.PropertyField(cellSizeProp);
        EditorGUILayout.Space(10);

        // 2. Culling Thresholds 영역 (핵심 커스텀 UI)
        EditorGUILayout.LabelField("⚙️ Culling Thresholds (a > b > 0)", EditorStyles.boldLabel);

        // b 값을 먼저 입력받고 방어 (물리)
        int newB = EditorGUILayout.IntField(new GUIContent("물리 연산 범위 (b)", "Collider가 활성화되는 격자 반경"), bProp.intValue);
        bProp.intValue = Mathf.Max(1, newB); // 항상 1 이상

        // a 값을 입력받고 방어 (렌더링)
        int newA = EditorGUILayout.IntField(new GUIContent("렌더링 범위 (a)", "GameObject가 켜지는 격자 반경"), aProp.intValue);
        aProp.intValue = Mathf.Max(bProp.intValue + 1, newA); // 항상 b보다 크게

        // 기획자를 위한 친절한 시각적 피드백 메시지
        EditorGUILayout.HelpBox(
            $"현재 설정 요약:\n" +
            $"• 플레이어 거리 0 ~ {bProp.intValue} : 물리(Collider) & 렌더링(Mesh) 모두 켜짐\n" +
            $"• 플레이어 거리 {bProp.intValue + 1} ~ {aProp.intValue} : 물리 꺼짐, 렌더링만 켜짐\n" +
            $"• 플레이어 거리 {aProp.intValue + 1} 이상 : 모든 객체 비활성화 (Culling)",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 3. Debug 영역
        EditorGUILayout.LabelField("🐞 Debug Visualization", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showDebugGridProp);
        if (showDebugGridProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(IsDrawSelectedProp);
            EditorGUILayout.PropertyField(debugPlaneHeightProp);
            EditorGUI.indentLevel--;
        }

        // 변경된 속성을 실제 객체에 적용합니다.
        serializedObject.ApplyModifiedProperties();
    }
}