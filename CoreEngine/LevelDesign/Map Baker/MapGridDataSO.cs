using UnityEngine;

namespace CoreEngine.LevelDesign
{
    [CreateAssetMenu(fileName = "NewMapGridData", menuName = MenuNamesSO.DefaultMenu + "/LevelDesign/Map Grid Data")]
    public class MapGridDataSO : ScriptableObject
    {
        [Header("Map Info")]
        public string sceneName; // 로드할 때 폴더 경로를 찾기 위한 이름

        [Header("File Path")]
        [Tooltip("이 맵의 이미지 타일들이 저장된 폴더 경로입니다.")]
        public string saveDirectory; // 🌟 추가됨

        [Header("Grid System")]
        public int totalCols;    // X축 타일 개수
        public int totalRows;    // Z축 타일 개수
        public Vector2 tileSize; // 타일 1장당 실제 3D 크기 (예: 720x480)

        [Header("World Bounds")]
        public Vector2 worldMinBounds; // 전체 맵의 가장 왼쪽 아래 좌표
        public Vector2 worldMaxBounds; // 전체 맵의 가장 오른쪽 위 좌표

        [Header("LOD (전체 맵)")]
        [Tooltip("M키를 눌렀을 때 보여줄 저해상도 전체 맵 이미지")]
        public Texture2D fullMapLOD;

        // 스크립트에서 매 프레임 연산하지 않도록 미리 계산해주는 프로퍼티
        public Vector2 WorldSize => worldMaxBounds - worldMinBounds;
        public Vector2 WorldCenter => (worldMaxBounds + worldMinBounds) * 0.5f;

        /// <summary>
        /// 캐릭터의 3D 월드 좌표를 넣으면, 현재 위치한 타일의 2D 인덱스(Col, Row)를 반환합니다.
        /// </summary>
        public Vector2Int GetGridIndex(Vector3 worldPos)
        {
            // 1. 전체 맵의 왼쪽 아래(Min)를 0,0 영점으로 삼았을 때의 상대 좌표 계산
            float relativeX = worldPos.x - worldMinBounds.x;
            float relativeZ = worldPos.z - worldMinBounds.y; // 3D Z축이 2D Bounds의 Y축에 해당

            // 2. 타일 사이즈로 나누어 내림(Floor) 처리하면 현재 격자 번호가 나옴
            int col = Mathf.FloorToInt(relativeX / tileSize.x);
            int row = Mathf.FloorToInt(relativeZ / tileSize.y);

            // 3. 맵 바깥으로 나갔을 때의 배열 인덱스 초과(Out of Bounds) 방지
            col = Mathf.Clamp(col, 0, totalCols - 1);
            row = Mathf.Clamp(row, 0, totalRows - 1);

            return new Vector2Int(col, row);
        }

        /// <summary>
        /// 특정 격자(Grid) 인덱스의 실제 3D 월드 최소/최대 바운드를 반환합니다.
        /// </summary>
        public Rect GetTileWorldBounds(Vector2Int gridIndex)
        {
            float minX = worldMinBounds.x + (gridIndex.x * tileSize.x);
            float minZ = worldMinBounds.y + (gridIndex.y * tileSize.y);

            // 해당 타일의 (최소 X, 최소 Z, 너비, 높이)
            return new Rect(minX, minZ, tileSize.x, tileSize.y);
        }
    }
}