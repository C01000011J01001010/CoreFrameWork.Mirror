using UnityEditor;
using UnityEngine;

namespace CoreEngine.Environment
{
    [CustomEditor(typeof(SpaceZoneCore))]
    public class SpaceZoneCoreEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SpaceZoneCore core = (SpaceZoneCore)target;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🌌 공간 마스터 코어", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이 객체의 Transform Scale이 모든 하위 모듈(외벽, 내벽)의 기준 크기가 됩니다.\n" +
                "씬 뷰에서 스케일 툴(R)로 조작하거나 아래 수치를 변경하세요.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            Vector3 newSize = EditorGUILayout.Vector3Field("마스터 공간 크기", core.zoneSize);

            if (EditorGUI.EndChangeCheck())
            {
                // 크기가 변경되면 1) 변수 저장, 2) 실제 Transform 반영, 3) 하위 모듈들에 갱신 방송
                Undo.RecordObject(core, "Change Zone Size");
                core.zoneSize = newSize;
                core.transform.localScale = newSize;

                core.TriggerRebuild();
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("전체 하위 모듈 강제 동기화 ⚡", GUILayout.Height(30)))
            {
                core.TriggerRebuild();
            }
        }
    }
}