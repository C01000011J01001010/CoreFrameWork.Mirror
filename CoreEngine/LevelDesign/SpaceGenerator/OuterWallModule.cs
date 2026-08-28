using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.LevelDesign
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpaceZoneCore))]
    public class OuterWallModule : BaseSpace
    {
        [HideInInspector][SerializeField] private bool _showOuterWalls = true;

        private SpaceZoneCore _core;

        public const string CONTAINER_NAME = "Outer";
        protected override string FolderName => CONTAINER_NAME;

        public bool ShowOuterWalls
        {
            get => _showOuterWalls;
            set { if (_showOuterWalls != value) { _showOuterWalls = value; UpdateWallVisuals(); } }
        }

        private void OnEnable()
        {
            _core = GetComponent<SpaceZoneCore>();
            _core.OnZoneModified += RebuildWalls;
        }

        private void OnDisable()
        {
            if (_core != null) _core.OnZoneModified -= RebuildWalls;
        }

        public void RebuildWalls()
        {
            if (_core == null) _core = GetComponent<SpaceZoneCore>();
            Vector3 pScale = _core.zoneSize;
            if (pScale.x <= 0 || pScale.y <= 0 || pScale.z <= 0) return;

            // 🌟 유저님 아이디어 적용: 폴더가 없으면 만들고, 있으면 재활용합니다.
            Transform folderTr = GetOrCreateContainer(FolderName);

            // 🌟 기존 객체의 Transform 수치만 가볍게 덮어씌웁니다. (메모리 재할당 0%)
            UpdateFace("OuterWall_Left", new Vector3(-0.5f - (0.5f / pScale.x), 0, 0), new Vector3(1f / pScale.x, 1f, 1f), folderTr);
            UpdateFace("OuterWall_Right", new Vector3(0.5f + (0.5f / pScale.x), 0, 0), new Vector3(1f / pScale.x, 1f, 1f), folderTr);
            UpdateFace("OuterWall_Bottom", new Vector3(0, -0.5f - (0.5f / pScale.y), 0), new Vector3(1f, 1f / pScale.y, 1f), folderTr);
            UpdateFace("OuterWall_Top", new Vector3(0, 0.5f + (0.5f / pScale.y), 0), new Vector3(1f, 1f / pScale.y, 1f), folderTr);
            UpdateFace("OuterWall_Back", new Vector3(0, 0, -0.5f - (0.5f / pScale.z)), new Vector3(1f, 1f, 1f / pScale.z), folderTr);
            UpdateFace("OuterWall_Front", new Vector3(0, 0, 0.5f + (0.5f / pScale.z)), new Vector3(1f, 1f, 1f / pScale.z), folderTr);

            UpdateWallVisuals();
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

        private void UpdateFace(string faceName, Vector3 localPos, Vector3 localScale, Transform parentFolder)
        {
            Transform faceTr = parentFolder.Find(faceName);
            GameObject faceObj;

            // 객체가 씬에 존재하지 않을 때만 1회 생성
            if (faceTr == null)
            {
                faceObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                faceObj.name = faceName;
                faceObj.transform.SetParent(parentFolder);
                if (faceObj.TryGetComponent(out BoxCollider bc)) bc.isTrigger = false;
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(faceObj, $"Create {faceName}");
#endif
            }
            else
            {
                faceObj = faceTr.gameObject;
#if UNITY_EDITOR
                Undo.RecordObject(faceObj.transform, "Update Wall Transform"); // Ctrl+Z를 위해 위치 변경만 기록
#endif
            }

            // 트랜스폼 수치만 갱신 (매우 빠르고 가벼움!)
            faceObj.SetActive(true);
            faceObj.transform.localPosition = localPos;
            faceObj.transform.localRotation = Quaternion.identity;
            faceObj.transform.localScale = localScale;
        }

        public void UpdateWallVisuals()
        {
            if (Application.isPlaying) _showOuterWalls = false;

            Transform folder = transform.Find(FolderName);
            if (folder == null) return;
            foreach (var renderer in folder.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = _showOuterWalls;
            }
        }
    }
}