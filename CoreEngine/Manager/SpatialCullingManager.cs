using System.Collections.Generic;
using UnityEngine;
using CoreEngine.EventBus;

namespace CoreEngine.Manager.Culling
{
    #region Culling Object
    public enum CullingType
    {
        Static,         // 절대 움직이지 않는 객체 (바위, 나무 등)
        PassiveDynamic, // 플레이어에 의해 수동적으로 움직이는 객체 (발판, 상자 등)
        ActiveDynamic,  // 능동적으로 움직이는 객체 (NPC, 몬스터 등)
    }

    public interface ICullingObject
    {
        CullingType cullingType { get; }
        Transform transform { get; } // Behaviour 자동

        /// <summary>
        /// 이 객체의 렌더러를 거리에 상관없이 항상 켜둘 것인가?
        /// </summary>
        bool IsVisualAlwaysOn { get; }
        void SetVisualActive(bool isActive);
        void SetPhysicsActive(bool isActive);
    }

    // 유니티 가짜 널(Fake Null) 방어용 구조체
    public struct CullingReference
    {
        public ICullingObject Interface;
        public Object UnityObject;

        public CullingReference(ICullingObject cullingObj)
        {
            Interface = cullingObj;
            UnityObject = cullingObj.transform;
        }

        public bool IsValid => UnityObject != null;
    }
    #endregion

    #region Events
    public struct CullingPlayerGridChangedEvent : IEvent
    {
        public Vector3Int PlayerGrid;
        public CullingPlayerGridChangedEvent(Vector3Int grid) => PlayerGrid = grid;
    }

    public struct CullingObjectRegistrationEvent : IEvent
    {
        public ICullingObject CullingObject;
        public bool IsRegister;
        public CullingObjectRegistrationEvent(ICullingObject obj, bool isRegister)
        {
            CullingObject = obj;
            IsRegister = isRegister;
        }
    }

    public struct CullingObjectMovedEvent : IEvent
    {
        public ICullingObject CullingObject;
        public Vector3Int OldGrid;
        public Vector3Int NewGrid;
        public CullingObjectMovedEvent(ICullingObject obj, Vector3Int oldGrid, Vector3Int newGrid)
        {
            CullingObject = obj;
            OldGrid = oldGrid;
            NewGrid = newGrid;
        }
    }
    #endregion

    public enum CullingAxis 
    { 
        OneD_X, 
        OneD_Y, 
        OneD_Z, 
        TwoD_XY, 
        TwoD_XZ, 
        //ThreeD_XYZ // 너무 비효율적이라 안씀
    }

    public class SpatialCullingManager : BaseManager
    {
        [Header("Grid Settings")]

        [Tooltip("차원에 따라 연산속도가 달라짐을 참고하시오")]
        [SerializeField] private CullingAxis cullingAxis = CullingAxis.TwoD_XZ;

        [Tooltip("a값의 한계에 도달했다면 저를 늘려보세요 ^^")]
        [SerializeField] private float cellSize = 10f;

        [Header("Culling Thresholds")]

        [Tooltip("GameObject 렌더링 한계 격자 개수")]
        [SerializeField] private int a = 3;

        [Tooltip("Collider 물리 연산 한계 격자 개수")]
        [SerializeField] private int b = 1;

        // 통합 격자 딕셔너리 (이제 완벽하게 CullingReference만 담습니다)
        protected Dictionary<Vector3Int, List<CullingReference>> gridDictionary = new();

        private Vector3Int _currentPlayerGrid = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue); // 초기 쓰레기값;

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus<CullingPlayerGridChangedEvent>.Subscribe(OnPlayerGridChanged);
            EventBus<CullingObjectRegistrationEvent>.Subscribe(OnCullingObjectRegistered);
            EventBus<CullingObjectMovedEvent>.Subscribe(OnCullingObjectMoved);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<CullingPlayerGridChangedEvent>.Unsubscribe(OnPlayerGridChanged);
            EventBus<CullingObjectRegistrationEvent>.Unsubscribe(OnCullingObjectRegistered);
            EventBus<CullingObjectMovedEvent>.Unsubscribe(OnCullingObjectMoved);
        }

        public Vector3Int GetGridKey(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / cellSize);
            int y = Mathf.FloorToInt(worldPos.y / cellSize);
            int z = Mathf.FloorToInt(worldPos.z / cellSize);

            return cullingAxis switch
            {
                CullingAxis.OneD_X => new Vector3Int(x, 0, 0),
                CullingAxis.OneD_Y => new Vector3Int(0, y, 0),
                CullingAxis.OneD_Z => new Vector3Int(0, 0, z),
                CullingAxis.TwoD_XY => new Vector3Int(x, y, 0),
                CullingAxis.TwoD_XZ => new Vector3Int(x, 0, z),
                //CullingAxis.ThreeD_XYZ => new Vector3Int(x, y, z),
                _ => new Vector3Int(x, 0, z)
            };
        }

        protected IEnumerable<Vector3Int> GetSurroundingGrids(Vector3Int center, int radius)
        {
            switch (cullingAxis)
            {
                case CullingAxis.OneD_X:
                    for (int x = -radius; x <= radius; x++) 
                        yield return new Vector3Int(center.x + x, 0, 0); 
                    break;

                case CullingAxis.OneD_Y:
                    for (int y = -radius; y <= radius; y++)
                        yield return new Vector3Int(0, center.y + y, 0); 
                    break;

                case CullingAxis.OneD_Z:
                    for (int z = -radius; z <= radius; z++)
                        yield return new Vector3Int(0, 0, center.z + z); 
                    break;

                case CullingAxis.TwoD_XY:
                    for (int x = -radius; x <= radius; x++) 
                        for (int y = -radius; y <= radius; y++) 
                            yield return new Vector3Int(center.x + x, center.y + y, 0); 
                    break;

                case CullingAxis.TwoD_XZ:
                    for (int x = -radius; x <= radius; x++) 
                        for (int z = -radius; z <= radius; z++) 
                            yield return new Vector3Int(center.x + x, 0, center.z + z); 
                    break;

                //case CullingAxis.ThreeD_XYZ:
                //    for (int x = -radius; x <= radius; x++) 
                //        for (int y = -radius; y <= radius; y++) 
                //            for (int z = -radius; z <= radius; z++) 
                //                yield return new Vector3Int(center.x + x, center.y + y, center.z + z); 
                //    break;

            }
        }

        private void OnPlayerGridChanged(CullingPlayerGridChangedEvent evt)
        {

            // 캐싱
            _currentPlayerGrid = evt.PlayerGrid;
            int maxRadius = a + 2;

            foreach (Vector3Int checkGrid in GetSurroundingGrids(_currentPlayerGrid, maxRadius))
            {
                if (gridDictionary.TryGetValue(checkGrid, out List<CullingReference> objects))
                {
                    int d = GetMaxDistance(_currentPlayerGrid, checkGrid);
                    ProcessCulling(objects, d);
                }
            }
        }

        private void OnCullingObjectRegistered(CullingObjectRegistrationEvent evt)
        {
            Vector3Int gridKey = GetGridKey(evt.CullingObject.transform.position);
            CullingReference reference = new CullingReference(evt.CullingObject);

            if (evt.IsRegister)
            {
                if (!gridDictionary.ContainsKey(gridKey))
                    gridDictionary[gridKey] = new List<CullingReference>();

                gridDictionary[gridKey].Add(reference);

                // 딕셔너리 등록 직후, 현재 플레이어 위치를 기준으로 즉시 계산
                // 플레이어가 아직 스폰되지 않았거나 너무 멀다면 자연스럽게 d 값이 커져서 즉시 Culling(OFF) 됨
                int d = GetMaxDistance(_currentPlayerGrid, gridKey);

                // 단일 객체만 리스트로 감싸서 판정 함수에 넘김 (O(1) 연산)
                List<CullingReference> singleList = new List<CullingReference> { reference };
                ProcessCulling(singleList, d);
            }
            else
            {
                if (gridDictionary.TryGetValue(gridKey, out List<CullingReference> list))
                {
                    list.RemoveAll(refObj => refObj.Interface == evt.CullingObject);
                    if (list.Count == 0) gridDictionary.Remove(gridKey);
                }
            }
        }

        private void OnCullingObjectMoved(CullingObjectMovedEvent evt)
        {
            if (gridDictionary.TryGetValue(evt.OldGrid, out List<CullingReference> oldList))
            {
                oldList.RemoveAll(refObj => refObj.Interface == evt.CullingObject);
                if (oldList.Count == 0) gridDictionary.Remove(evt.OldGrid);
            }

            if (!gridDictionary.ContainsKey(evt.NewGrid))
            {
                gridDictionary[evt.NewGrid] = new List<CullingReference>();
            }
            gridDictionary[evt.NewGrid].Add(new CullingReference(evt.CullingObject));

            int d = GetMaxDistance(_currentPlayerGrid, evt.NewGrid);

            List<CullingReference> singleObjList = new List<CullingReference> { new CullingReference(evt.CullingObject) };
            ProcessCulling(singleObjList, d);
        }

        private void ProcessCulling(List<CullingReference> objects, int d)
        {
            if (d <= b) SetCollidersActive(objects, true);
            else if (d > b + 1) SetCollidersActive(objects, false);

            if (d <= a) SetVisualsActive(objects, true);
            else if (d > a + 1) SetVisualsActive(objects, false);
        }

        private void SetVisualsActive(List<CullingReference> objects, bool isActive)
        {
            CullingReference[] snapshot = objects.ToArray();
            foreach (var refObj in snapshot)
            {
                // 유니티 객체가 제대로 존재하는지 체크
                if (!refObj.IsValid) continue;

                // 안보이게 처리하라는 명령인데 항시 켜져야하는 객체인지 체크
                if (!isActive && refObj.Interface.IsVisualAlwaysOn) continue;

                refObj.Interface.SetVisualActive(isActive);
            }
        }

        private void SetCollidersActive(List<CullingReference> objects, bool isActive)
        {
            CullingReference[] snapshot = objects.ToArray();
            foreach (var refObj in snapshot)
            {
                if (!refObj.IsValid) continue;
                refObj.Interface.SetPhysicsActive(isActive);
            }
        }
        private int GetMaxDistance(Vector3Int from, Vector3Int to)
        {
            return Mathf.Max(Mathf.Abs(from.x - to.x),
                            Mathf.Abs(from.y - to.y),
                            Mathf.Abs(from.z - to.z));
        }

#if UNITY_EDITOR
        
        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugGrid = true;
        [SerializeField] private bool IsDrawSelected = false;
        [SerializeField] private float debugPlaneHeight = 0f;

        private void OnDrawGizmos() { if (!IsDrawSelected) DrawGrid(); }
        private void OnDrawGizmosSelected() { if (IsDrawSelected) DrawGrid(); }

        private void DrawGrid()
        {
            if (!showDebugGrid || !Application.isPlaying) return;

            int maxRadius = a + 2;
            foreach (Vector3Int checkGrid in GetSurroundingGrids(_currentPlayerGrid, maxRadius))
            {
                int d = Mathf.Max(Mathf.Abs(_currentPlayerGrid.x - checkGrid.x),
                                  Mathf.Abs(_currentPlayerGrid.y - checkGrid.y),
                                  Mathf.Abs(_currentPlayerGrid.z - checkGrid.z));

                if (d == 0) Gizmos.color = Color.cyan;
                else if (d <= b) Gizmos.color = Color.green;
                else if (d <= a) Gizmos.color = Color.yellow;
                else if (d == a + 1 || d == a + 2) Gizmos.color = Color.red;

                DrawGridCellGizmo(checkGrid);
            }
        }

        private void DrawGridCellGizmo(Vector3Int gridKey)
        {
            float halfCell = cellSize * 0.5f;
            Vector3 center = new Vector3(gridKey.x * cellSize + halfCell, gridKey.y * cellSize + halfCell, gridKey.z * cellSize + halfCell);
            Vector3 size = Vector3.one * cellSize;

            switch (cullingAxis)
            {
                case CullingAxis.OneD_X: center.y = debugPlaneHeight; center.z = debugPlaneHeight; size.y = 1000f; size.z = 0f; break;
                case CullingAxis.OneD_Y: center.x = debugPlaneHeight; center.z = debugPlaneHeight; size.x = 1000f; size.z = 0f; break;
                case CullingAxis.OneD_Z: center.x = debugPlaneHeight; center.y = debugPlaneHeight; size.x = 1000f; size.y = 0f; break;
                case CullingAxis.TwoD_XY: center.z = debugPlaneHeight; size.z = 0f; break;
                case CullingAxis.TwoD_XZ: center.y = debugPlaneHeight; size.y = 0f; break;
            }

            Gizmos.DrawWireCube(center, size);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
            Gizmos.DrawCube(center, size);
        }
#endif


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // cellSize사이즈가 음수가 되는거 방지
            cellSize = Mathf.Max(1, cellSize);

            // 1. b는 무조건 1 이상이어야 함 (물리 연산 최소 반경)
            b = Mathf.Max(1, b);

            // 2. 최하옵 PC를 위한 극한의 최적화 한도 (CPU Cache Miss 방지)
            int maxAllowedA = 7; // 기본값 2D 기준

            switch (cullingAxis)
            {
                // visual 범위값을 a값, 차원을 x라고 했을 때
                // 탐색범위 R = a+2
                // 플레이어가 격자를 이동할때마다 (2R+1)^x == (2a+5)^x 의 수행시간 발생
                // 그러므로 차원에 따라 a의 범위를 제한해야함
                // 탐색을 위한 절대범위를 늘리고싶다면 cellsize를 조작해야함
                case CullingAxis.OneD_X:
                case CullingAxis.OneD_Y:
                case CullingAxis.OneD_Z:
                    maxAllowedA = 30; // 탐색 횟수: 65번 (매우 가벼움)
                    break;
                case CullingAxis.TwoD_XY:
                case CullingAxis.TwoD_XZ:
                    maxAllowedA = 7;  // 탐색 횟수: 361번 (최하옵 2D 안전선)
                    break;
                //case CullingAxis.ThreeD_XYZ:
                //    maxAllowedA = 2;  // 탐색 횟수: 729번 (최하옵 3D 마지노선!)
                //    break;
            }

            // 3. a는 'b + 1' 보다 크거나 같아야 하고, 기기 한계치(maxAllowedA)를 넘을 수 없음
            a = Mathf.Clamp(a, b + 1, maxAllowedA);

            // 4. 기획자가 억지로 b를 너무 높여서 a의 공간을 침범하는 경우 강제 교정
            if (b >= maxAllowedA)
            {
                b = maxAllowedA - 1;
                a = maxAllowedA;
            }
        }
#endif
    }
}