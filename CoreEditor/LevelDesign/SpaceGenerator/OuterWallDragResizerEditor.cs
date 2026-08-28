using UnityEditor;
using UnityEngine;
using CoreEngine.LevelDesign;

namespace CoreEditor.LevelDesign
{
    /// <summary>
    /// OuterWallDragResizer 컴포넌트의 유니티 인스펙터 및 씬 뷰(Scene View) 조작을 커스터마이징하는 클래스입니다.
    /// 에디터 폴더(Editor) 내부에 위치해야 하며, 실제 게임 빌드(exe)에는 포함되지 않습니다.
    /// </summary>
    [CustomEditor(typeof(OuterWallDragResizer))]
    public class OuterWallDragResizerEditor : Editor
    {
        // 조작할 타겟 런타임 스크립트를 연결할 변수
        private OuterWallDragResizer _resizer;

        /// <summary>
        /// 인스펙터에서 이 컴포넌트가 클릭되어 활성화될 때 딱 한 번 호출됩니다.
        /// </summary>
        private void OnEnable()
        {
            // target은 Editor 클래스에 내장된 변수로, 현재 선택된 오브젝트를 의미합니다.
            _resizer = (OuterWallDragResizer)target;
        }

        /// <summary>
        /// 유니티 에디터 우측의 '인스펙터(Inspector)' 창에 보여질 UI를 그리는 함수입니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🖱️ 외벽 실시간 마우스 드래그 툴", EditorStyles.boldLabel);

            // 기획자나 레벨 디자이너가 툴 사용법을 알 수 있도록 안내문 표시
            EditorGUILayout.HelpBox(
                "씬 뷰(Scene View)에서 직사각형의 각 표면 정중앙을 보세요.\n" +
                "오직 바깥쪽 직각 방향으로 향하는 '단 1개의 깨끗한 화살표'만 활성화됩니다.\n" +
                "유니티 표준 색상(X-빨강, Y-초록, Z-파랑)이 적용되어 직관적인 편집이 가능합니다.",
                MessageType.Info
            );
            EditorGUILayout.Space(5);

            // [변경 감지 시작] 여기서부터 그려지는 UI 조작을 유니티가 감시합니다.
            EditorGUI.BeginChangeCheck();

            // 체크박스(토글) UI 생성
            bool toggleHandles = EditorGUILayout.Toggle("화살표 핸들 켜기", _resizer.showDragHandles);

            // [변경 감지 끝] 사용자가 방금 체크박스를 클릭해서 상태가 변했다면?
            if (EditorGUI.EndChangeCheck())
            {
                // Ctrl+Z(실행 취소)를 위해 변경 사항을 유니티 역사(History)에 기록
                Undo.RecordObject(_resizer, "Toggle Drag Handles");
                _resizer.showDragHandles = toggleHandles;

                // 씬 뷰에 마우스를 올리지 않아도, 체크박스를 누르는 즉시 씬 뷰 화면을 강제로 새로고침(화살표 즉각 On/Off)
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// 유니티 에디터 중앙의 '씬 뷰(Scene View)'에 3D 그래픽(기즈모, 핸들)을 그릴 때 호출되는 마법의 함수입니다.
        /// </summary>
        private void OnSceneGUI()
        {
            // 🛡️ [핵심 방어막] 스크립트가 지워졌거나, 컴포넌트 체크박스를 껐거나, 토글이 꺼져있으면 아예 화살표를 그리지 않고 탈출!
            if (_resizer == null || !_resizer.enabled || !_resizer.showDragHandles) return;

            // 외벽들이 모여있는 부모 폴더 객체를 찾습니다.
            Transform outerFolder = _resizer.transform.Find(OuterWallModule.CONTAINER_NAME);
            if (outerFolder == null) return;

            // 폴더 안에 있는 6개의 벽면(자식들)을 하나씩 꺼내보며 화살표를 붙여줍니다.
            for (int i = 0; i < outerFolder.childCount; i++)
            {
                Transform wallFace = outerFolder.GetChild(i);

                // 꺼져있는 벽면이면 무시
                if (!wallFace.gameObject.activeSelf) continue;

                Vector3 localNormal = Vector3.zero; // 화살표가 뻗어나갈 방향
                Color handleColor = Color.white;    // 화살표의 색깔

                // 🎨 벽면의 이름표를 보고, 어떤 방향으로 무슨 색 화살표를 그릴지 결정합니다.
                switch (wallFace.name)
                {
                    case "OuterWall_Left":
                    case "OuterWall_Right":
                        localNormal = (wallFace.name == "OuterWall_Left") ? Vector3.left : Vector3.right;
                        handleColor = Color.red; // X축(좌우)은 빨간색
                        break;
                    case "OuterWall_Bottom":
                    case "OuterWall_Top":
                        localNormal = (wallFace.name == "OuterWall_Bottom") ? Vector3.down : Vector3.up;
                        handleColor = Color.green; // Y축(위아래)은 초록색
                        break;
                    case "OuterWall_Back":
                    case "OuterWall_Front":
                        localNormal = (wallFace.name == "OuterWall_Back") ? Vector3.back : Vector3.forward;
                        handleColor = Color.blue; // Z축(앞뒤)은 파란색
                        break;
                }

                // 화살표가 그려질 실제 3D 월드 좌표 (해당 벽면의 정중앙)
                Vector3 worldPos = wallFace.position;

                // 로컬 방향(예: 왼쪽)을, 부모 객체가 뱅글뱅글 회전해 있더라도 알맞은 진짜 월드 방향으로 변환해줍니다.
                Vector3 worldDir = _resizer.transform.TransformDirection(localNormal);

                // 📷 카메라가 줌아웃해서 멀어져도, 화살표가 화면에서 항상 일정한 크기로 예쁘게 보이도록 계산합니다.
                float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.7f;

                // 🖌️ [색상 덮어쓰기] 유니티 에디터의 붓(Handles) 색상은 공용이므로, 현재 색상을 잠시 저장해두고 내 색상으로 바꿉니다.
                Color originalColor = Handles.color;
                Handles.color = handleColor;

                // [드래그 감지 시작] 유저가 화살표를 마우스로 잡고 끄는지 감시합니다.
                EditorGUI.BeginChangeCheck();

                // 🎯 대망의 화살표 그리기! (Handles.Slider는 지정된 축으로만 이동할 수 있는 단방향 화살표입니다)
                // 유저가 드래그를 마치면, 화살표가 이동한 새로운 3D 좌표를 newWorldPos에 반환합니다.
                Vector3 newWorldPos = Handles.Slider(worldPos, worldDir, handleSize, Handles.ArrowHandleCap, 0.1f);

                // [드래그 감지 끝] 유저가 마우스로 화살표 위치를 움직였다면?
                if (EditorGUI.EndChangeCheck())
                {
                    // 📐 [수학 마법: 내적(Dot Product)]
                    // 유저의 손이 떨려서 마우스가 대각선으로 움직였더라도, 
                    // 우리가 지정한 화살표 방향(worldDir)으로 '정확히 얼만큼' 이동했는지 수직 투영 길이만 쏙 뽑아냅니다.
                    float deltaWorld = Vector3.Dot(newWorldPos - worldPos, worldDir);

                    // Ctrl+Z(실행 취소)를 위해 부모 객체들의 현재 상태를 유니티 역사에 기록
                    Undo.RecordObject(_resizer.transform, "Drag Face Move Delta");
                    Undo.RecordObject(_resizer.gameObject.transform, "Anchor Base Shift Vector");

                    // 추출해낸 정확한 이동 거리(deltaWorld)를 런타임 스크립트에게 넘겨 실제 큐브 크기를 변형시킵니다.
                    _resizer.ApplyAxisDelta(wallFace.name, deltaWorld);
                }

                // 🖌️ [색상 원복] 내 화살표를 다 그렸으니, 다른 기즈모들이 빨간색/파란색으로 물들지 않도록 붓 색깔을 원래대로 돌려놓습니다.
                Handles.color = originalColor;
            }
        }
    }
}