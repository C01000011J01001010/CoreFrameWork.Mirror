using CoreEngine.LevelDesign;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.LevelDesign
{
    [CustomEditor(typeof(InnerZoneModule))]
    public class InnerZoneModuleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            InnerZoneModule module = (InnerZoneModule)target;
            SpaceZoneCore core = module.GetComponent<SpaceZoneCore>();

            Vector3 pScale = core != null ? core.zoneSize : module.transform.localScale;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("📐 내부 효과 구역 분할 모듈", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "슬라이더를 조작하여 내부 A, B, C 구역의 크기를 조절합니다.\n" +
                "Zone B는 지정한 폭만큼 중앙을 비우고 양쪽 외곽을 채웁니다.",
                MessageType.None);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            // 1. 수치 조절 슬라이더
            float newAY = EditorGUILayout.Slider("Zone A 시작 Y축 고도", module.ZoneA_StartY, 0f, pScale.y);
            float newBX = EditorGUILayout.Slider("Zone B 중앙 공백 너비", module.ZoneB_StartXAbs, 0f, pScale.x * 0.5f);
            float newCY = EditorGUILayout.Slider("Zone C 종료 Y축 고도", module.ZoneC_EndY, 0f, pScale.y);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("👁️ 구역별 가시성(Visibility) 설정", EditorStyles.boldLabel);

            // 2. 마스터 토글
            bool toggleMaster = EditorGUILayout.Toggle("전체 구역 메쉬 켜기", module.ShowInnerZones);

            // 3. 개별 종속 토글 (마스터가 꺼지면 회색으로 비활성화됨)
            bool toggleA = module.ShowZoneA;
            bool toggleB = module.ShowZoneB;
            bool toggleC = module.ShowZoneC;

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledGroupScope(!toggleMaster))
            {
                toggleA = EditorGUILayout.Toggle("🟥 Zone A 켜기", module.ShowZoneA);
                toggleB = EditorGUILayout.Toggle("🟩 Zone B 켜기", module.ShowZoneB);
                toggleC = EditorGUILayout.Toggle("🟦 Zone C 켜기", module.ShowZoneC);
            }
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(module, "Modify Inner Zone Parameters");

                module.ZoneA_StartY = newAY;
                module.ZoneB_StartXAbs = newBX;
                module.ZoneC_EndY = newCY;

                // 가시성 토글 상태 적용
                module.ShowInnerZones = toggleMaster;
                module.ShowZoneA = toggleA;
                module.ShowZoneB = toggleB;
                module.ShowZoneC = toggleC;

                if (core != null)
                    core.TriggerRebuild();
                else
                    module.RebuildZones();
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("수동 갱신 (에러 복구용) 🛠️", GUILayout.Height(30)))
            {
                module.RebuildZones();
            }
        }
    }
}