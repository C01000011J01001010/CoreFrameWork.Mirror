using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CoreEditor.EditorTools
{
    public class CanvasMonitorWindow : EditorWindow
    {
        // 캔버스 데이터를 저장할 리스트
        private List<Canvas> _activeCanvases = new List<Canvas>();
        private List<Canvas> _inactiveCanvases = new List<Canvas>();

        // 스크롤 뷰 위치 추적용 변수
        private Vector2 _activeScrollPosition;
        private Vector2 _inactiveScrollPosition;

        // GUI 스타일
        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;

        [MenuItem(Constants.ToolRootUi+"Canvas Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<CanvasMonitorWindow>("Canvas Monitor");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 에디터 창이 열릴 때 한 번 초기 탐색 실행
            RefreshCanvasData();
        }

        private void OnGUI()
        {
            // 스타일 초기화 (OnGUI 내부에서 해야 안전함)
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };

                _itemStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true // 색상 태그 지원
                };
            }

            // 상단 컨트롤 영역
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("UI Rendering Optimizer", EditorStyles.largeLabel);

            if (GUILayout.Button("탐색 (Refresh Canvas Data)", GUILayout.Height(30)))
            {
                RefreshCanvasData();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 요약 정보 표시
            int totalCount = _activeCanvases.Count + _inactiveCanvases.Count;
            EditorGUILayout.LabelField($"전체 캔버스 객체: {totalCount}개", _headerStyle);

            EditorGUILayout.Space();

            // ==========================================
            // 1. 활성화된 캔버스 리스트 (Active)
            // ==========================================
            EditorGUILayout.LabelField($"<color=#32CD32>활성화된 캔버스: {_activeCanvases.Count}개</color>", _itemStyle);

            // 리스트를 보기 좋게 박스 안에 스크롤뷰로 배치
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _activeScrollPosition = EditorGUILayout.BeginScrollView(_activeScrollPosition, GUILayout.MaxHeight(150));

            if (_activeCanvases.Count == 0)
            {
                EditorGUILayout.LabelField("활성화된 캔버스가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var canvas in _activeCanvases)
                {
                    DrawCanvasItem(canvas);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15); // 리스트 간격

            // ==========================================
            // 2. 비활성화된 캔버스 리스트 (Inactive)
            // ==========================================
            EditorGUILayout.LabelField($"<color=#FF4500>비활성화된 캔버스: {_inactiveCanvases.Count}개</color>", _itemStyle);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _inactiveScrollPosition = EditorGUILayout.BeginScrollView(_inactiveScrollPosition);

            if (_inactiveCanvases.Count == 0)
            {
                EditorGUILayout.LabelField("비활성화된 캔버스가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var canvas in _inactiveCanvases)
                {
                    DrawCanvasItem(canvas);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 씬 내의 모든 캔버스를 찾아 활성/비활성 상태로 분류합니다.
        /// </summary>
        private void RefreshCanvasData()
        {
            _activeCanvases.Clear();
            _inactiveCanvases.Clear();

            // Resources.FindObjectsOfTypeAll는 꺼져있는 오브젝트, 프리팹까지 다 찾으므로
            // 현재 씬에 있는 것만 안전하게 찾기 위해 GameObject.FindObjectsOfType 사용
            // 단, 꺼져있는 객체도 찾아야 하므로 includeInactive: true 옵션 사용 (유니티 2020.3 이상)
            Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();

            foreach (Canvas canvas in allCanvases)
            {
                // 프리팹 에셋(프로젝트 폴더 내)이 아닌, 실제 씬(하이라키)에 존재하는 것만 필터링
                if (canvas.gameObject.scene.IsValid() == false) continue;
                // 에디터 내부 시스템 UI 캔버스 제외 (HideFlags.HideAndDontSave 등)
                if ((canvas.gameObject.hideFlags & HideFlags.HideInHierarchy) != 0) continue;

                if (canvas.gameObject.activeInHierarchy)
                {
                    _activeCanvases.Add(canvas);
                }
                else
                {
                    _inactiveCanvases.Add(canvas);
                }
            }

            // 하이라키 순서대로 정렬 (선택 사항)
            _activeCanvases.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            _inactiveCanvases.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }

        /// <summary>
        /// 리스트에 개별 캔버스 항목을 그립니다. 클릭 시 하이라키에서 선택되도록 합니다.
        /// </summary>
        private void DrawCanvasItem(Canvas canvas)
        {
            if (canvas == null) return;

            EditorGUILayout.BeginHorizontal();

            // 객체 선택 버튼
            if (GUILayout.Button(canvas.gameObject.name, EditorStyles.label))
            {
                // 라벨을 클릭하면 하이라키에서 해당 객체를 포커스(핑) 해줌
                EditorGUIUtility.PingObject(canvas.gameObject);
                Selection.activeGameObject = canvas.gameObject;
            }

            // 추가 정보: 렌더 모드 표시 (Screen Space / World Space 등)
            GUILayout.FlexibleSpace(); // 오른쪽 정렬을 위한 여백
            EditorGUILayout.LabelField($"[{canvas.renderMode}]", EditorStyles.miniLabel, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }
    }
}