using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.Environment
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpaceZoneCore))]
    public class InnerZoneModule : BaseEnvironment
    {
        [HideInInspector][SerializeField] private float _zoneA_StartY = 6f;
        [HideInInspector][SerializeField] private float _zoneB_StartXAbs = 3f;
        [HideInInspector][SerializeField] private float _zoneC_EndY = 4f;

        [HideInInspector][SerializeField] private bool _showInnerZones = true; // 마스터 토글

        // 🌟 개별 구역 가시성 상태 저장 변수 추가
        [HideInInspector][SerializeField] private bool _showZoneA = true;
        [HideInInspector][SerializeField] private bool _showZoneB = true;
        [HideInInspector][SerializeField] private bool _showZoneC = true;

        private SpaceZoneCore _core;
        public const string CONTAINER_NAME = "Inner";
        protected override string FolderName => CONTAINER_NAME;

        #region Properties
        public float ZoneA_StartY { get => _zoneA_StartY; set => _zoneA_StartY = value; }
        public float ZoneB_StartXAbs { get => _zoneB_StartXAbs; set => _zoneB_StartXAbs = value; }
        public float ZoneC_EndY { get => _zoneC_EndY; set => _zoneC_EndY = value; }

        public bool ShowInnerZones
        {
            get => _showInnerZones;
            set { if (_showInnerZones != value) { _showInnerZones = value; UpdateZoneVisuals(); } }
        }

        // 🌟 개별 프로퍼티 추가 (변경 시 즉시 렌더러 업데이트)
        public bool ShowZoneA { get => _showZoneA; set { if (_showZoneA != value) { _showZoneA = value; UpdateZoneVisuals(); } } }
        public bool ShowZoneB { get => _showZoneB; set { if (_showZoneB != value) { _showZoneB = value; UpdateZoneVisuals(); } } }
        public bool ShowZoneC { get => _showZoneC; set { if (_showZoneC != value) { _showZoneC = value; UpdateZoneVisuals(); } } }
        #endregion

        private void OnEnable()
        {
            _core = GetComponent<SpaceZoneCore>();
            _core.OnZoneModified += RebuildZones;
        }

        private void OnDisable()
        {
            if (_core != null) _core.OnZoneModified -= RebuildZones;
        }

        public void RebuildZones()
        {
            if (_core == null) _core = GetComponent<SpaceZoneCore>();
            Vector3 pScale = _core.zoneSize;
            if (pScale.x <= 0 || pScale.y <= 0 || pScale.z <= 0) return;

            Transform folderTr = GetOrCreateContainer(FolderName);

            // [Zone A]
            float sizeYA = pScale.y - _zoneA_StartY;
            Vector3 centerA = new Vector3(0f, (-pScale.y * 0.5f) + _zoneA_StartY + (sizeYA * 0.5f), 0f);
            UpdateZoneFace("_Zone_A", centerA, new Vector3(pScale.x, sizeYA, pScale.z), Color.red, folderTr, true);

            // [Zone B - 좌/우 대칭]
            float sizeXB = (pScale.x * 0.5f) - _zoneB_StartXAbs;
            bool isZoneBActive = sizeXB > 0f;

            Vector3 centerB_Left = new Vector3((-pScale.x * 0.5f - _zoneB_StartXAbs) * 0.5f, 0f, 0f);
            UpdateZoneFace("_Zone_B_Left", centerB_Left, new Vector3(sizeXB, pScale.y, pScale.z), Color.green, folderTr, isZoneBActive);

            Vector3 centerB_Right = new Vector3((pScale.x * 0.5f + _zoneB_StartXAbs) * 0.5f, 0f, 0f);
            UpdateZoneFace("_Zone_B_Right", centerB_Right, new Vector3(sizeXB, pScale.y, pScale.z), Color.green, folderTr, isZoneBActive);

            // [Zone C]
            Vector3 centerC = new Vector3(0f, (-pScale.y * 0.5f) + (_zoneC_EndY * 0.5f), 0f);
            UpdateZoneFace("_Zone_C", centerC, new Vector3(pScale.x, _zoneC_EndY, pScale.z), Color.blue, folderTr, true);

            UpdateZoneVisuals();
        }

        private Transform GetOrCreateContainer(string containerName)
        {
            Transform container = transform.Find(containerName);
            if (container == null)
            {
                GameObject obj = new GameObject(containerName);
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(obj, $"Create {containerName}");
#endif
                container = obj.transform;
                container.SetParent(transform);
            }
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
            return container;
        }

        private void UpdateZoneFace(string zoneName, Vector3 localCenter, Vector3 localSize, Color zoneColor, Transform parent, bool isVisibleAndActive)
        {
            Transform zoneTr = parent.Find(zoneName);
            GameObject zoneObj;

            if (zoneTr == null)
            {
                zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                zoneObj.name = zoneName;
                zoneObj.transform.SetParent(parent);
                if (zoneObj.TryGetComponent(out BoxCollider bc)) bc.isTrigger = true;

                if (zoneObj.TryGetComponent(out MeshRenderer mr))
                {
                    Material tempMat = new Material(Shader.Find("Sprites/Default"));
                    tempMat.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.25f);
                    mr.sharedMaterial = tempMat;
                }
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(zoneObj, $"Create {zoneName}");
#endif
            }
            else
            {
                zoneObj = zoneTr.gameObject;
#if UNITY_EDITOR
                Undo.RecordObject(zoneObj.transform, "Update Inner Transform");
#endif
            }

            zoneObj.SetActive(isVisibleAndActive);

            if (isVisibleAndActive)
            {
                Vector3 pScale = _core.zoneSize;
                zoneObj.transform.localPosition = new Vector3(localCenter.x / pScale.x, localCenter.y / pScale.y, localCenter.z / pScale.z);
                zoneObj.transform.localRotation = Quaternion.identity;
                zoneObj.transform.localScale = new Vector3(localSize.x / pScale.x, localSize.y / pScale.y, localSize.z / pScale.z);
            }
        }

        public void UpdateZoneVisuals()
        {
            Transform folder = transform.Find(FolderName);
            if (folder == null) return;

            // 🌟 마스터 토글이 꺼져있거나, 게임 플레이 중이면 무조건 모두 끕니다.
            bool isMasterOn = _showInnerZones && !Application.isPlaying;

            foreach (Transform child in folder)
            {
                if (child.TryGetComponent(out MeshRenderer mr))
                {
                    if (!isMasterOn)
                    {
                        mr.enabled = false;
                        continue;
                    }

                    // 🌟 개별 구역별로 이름(Prefix)을 검사하여 각자의 토글 상태를 매핑합니다.
                    if (child.name == "_Zone_A") mr.enabled = _showZoneA;
                    else if (child.name.StartsWith("_Zone_B")) mr.enabled = _showZoneB; // Left와 Right 동시 제어
                    else if (child.name == "_Zone_C") mr.enabled = _showZoneC;
                }
            }
        }
    }
}