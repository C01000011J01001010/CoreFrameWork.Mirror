using UnityEngine;

namespace CoreEngine.Environment
{
    /// <summary>
    /// [모듈형 확장 플러그인]
    /// 자식 외벽(Outer Wall)의 단일 축 조작 변위량을 역산하여, 반대쪽 면을 고정시킨 채
    /// 마스터 코어(SpaceZoneCore)의 크기와 위치를 실시간으로 스케일링하는 컴포넌트입니다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpaceZoneCore))]
    [RequireComponent(typeof(OuterWallModule))] // 요구사항 반영: 외벽 생성 모듈 필수 제약(Require)
    public class OuterWallDragResizer : BaseEnvironment
    {
        private SpaceZoneCore _core;

        // 부모 클래스 자폭 루틴용 명세 (본 컴포넌트는 서브 에디팅 유틸이므로 공백 처리)
        protected override string FolderName => "";

        // 🌟 새로 추가할 기즈모 제어 토글 변수
        [Header("⚙️ 에디터 제어")]
        public bool showDragHandles = true;

        private void OnEnable()
        {
            _core = GetComponent<SpaceZoneCore>();
        }

        /// <summary>
        /// 에디터 스크립트에서 특정 벽면의 직각 드래그를 감지하면 실시간으로 호출하는 마스터 연산기입니다.
        /// </summary>
        /// <param name="wallName">조작된 외벽의 고유 명칭</param>
        /// <param name="deltaWorld">마우스 드래그로 이동한 월드 좌표계 기준 실질 거리 변위량</param>
        public void ApplyAxisDelta(string wallName, float deltaWorld)
        {
            if (_core == null) _core = GetComponent<SpaceZoneCore>();

            Vector3 currentSize = _core.zoneSize;
            Vector3 newSize = currentSize;
            Vector3 localTranslateDir = Vector3.zero;

            // 🌟 요구사항 반영: 어떤 벽면을 잡았느냐에 따라 크기를 가산하고 이동할 방향 벡터를 매핑합니다.
            switch (wallName)
            {
                case "OuterWall_Left":
                    newSize.x += deltaWorld;
                    localTranslateDir = Vector3.left;
                    break;
                case "OuterWall_Right":
                    newSize.x += deltaWorld;
                    localTranslateDir = Vector3.right;
                    break;
                case "OuterWall_Bottom":
                    newSize.y += deltaWorld;
                    localTranslateDir = Vector3.down;
                    break;
                case "OuterWall_Top":
                    newSize.y += deltaWorld;
                    localTranslateDir = Vector3.up;
                    break;
                case "OuterWall_Back":
                    newSize.z += deltaWorld;
                    localTranslateDir = Vector3.back;
                    break;
                case "OuterWall_Front":
                    newSize.z += deltaWorld;
                    localTranslateDir = Vector3.forward;
                    break;
            }

            // 음수 크기로 찌그러져 씬 뷰에서 뒤집히거나 소실되는 오버플로우 한계 차단
            if (newSize.x < 0.1f) newSize.x = 0.1f;
            if (newSize.y < 0.1f) newSize.y = 0.1f;
            if (newSize.z < 0.1f) newSize.z = 0.1f;

            // 한계치에 걸렸을 경우를 대비해 진짜 변형된 실질 델타값 재추출
            float actualDeltaX = newSize.x - currentSize.x;
            float actualDeltaY = newSize.y - currentSize.y;
            float actualDeltaZ = newSize.z - currentSize.z;

            // 1. 크기(Scale) 먼저 전격 반영
            _core.zoneSize = newSize;
            _core.transform.localScale = newSize;

            // 2. 🌟 알고리즘 핵심: 늘어난 크기 배율의 정확히 '절반'만큼 로컬 축 방향으로 이동하여 반대편 모서리를 박아둡니다.
            Vector3 localOffset = Vector3.zero;
            if (localTranslateDir == Vector3.left || localTranslateDir == Vector3.right) localOffset = localTranslateDir * actualDeltaX * 0.5f;
            if (localTranslateDir == Vector3.down || localTranslateDir == Vector3.up) localOffset = localTranslateDir * actualDeltaY * 0.5f;
            if (localTranslateDir == Vector3.back || localTranslateDir == Vector3.forward) localOffset = localTranslateDir * actualDeltaZ * 0.5f;

            // 부모의 로컬 방향을 월드로 무너뜨리지 않고 자석 슬라이딩 변형을 전파합니다.
            _core.transform.Translate(localOffset, Space.Self);

            // 3. 변경 사항을 모든 하위 조립 모듈(내벽 등)에게 즉시 방송하여 실시간 고속 동기화
            _core.TriggerRebuild();
        }
    }
}