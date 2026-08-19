using UnityEditor;
using UnityEngine;

namespace CoreEngine.Environment
{
    [CustomEditor(typeof(OuterWallModule))]
    public class OuterWallModuleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            OuterWallModule module = (OuterWallModule)target;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🧱 외곽 투명벽 모듈", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "SpaceZoneCore의 크기 변화 방송을 구독하여, " +
                "플레이어를 가두는 두께 1짜리 외곽 물리 벽을 자동 정렬합니다.",
                MessageType.None);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            bool toggle = EditorGUILayout.Toggle("외벽 메쉬 가이드 켜기", module.ShowOuterWalls);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(module, "Toggle Outer Walls");
                module.ShowOuterWalls = toggle;
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("수동 갱신 (에러 복구용) 🛠️", GUILayout.Height(30)))
            {
                module.RebuildWalls();
            }
        }
    }
}