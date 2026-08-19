using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.LevelDesign
{
    #region [Enums] 지형 및 베이킹 옵션 정의

    //public enum MapDimension { _2D, _3D }
    public enum MapProjectionPlane { XZ, XY }

    public enum MapDepthSteps
    {
        None = 0,   // 명도 양자화 없이 원본 매터리얼 그대로 렌더링
        Step_1 = 1, // 양자화 없이 단색(Layer 색상)으로 렌더링
        Step_2 = 2, // 명도 양자화 시작 (2단계)
        Step_4 = 4, Step_8 = 8, Step_12 = 12, Step_16 = 16,
        Step_24 = 24, Step_32 = 32, Step_48 = 48, Step_64 = 64,
    }

    public enum MapResolution
    {
        Res_512 = 512, Res_1024 = 1024, Res_2048 = 2048, Res_4096 = 4096
    }

    #endregion

    #region [Structs & Classes] 레이어별 세부 설정

    [System.Serializable]
    public struct LayerColorPair
    {
        public string layerName;
        public Color color;
    }

    [System.Serializable]
    public class LayerOutlineSetting
    {
        public string layerName;

        [Tooltip("해당 레이어 오브젝트에 외곽선을 생성할지 여부")]
        public bool isUse;

        public Color outlineColor = Color.black;
        [Range(1, 5)] public int outlineThickness = 2;
        [Range(0f, 0.05f)] public float depthThreshold = 0.001f;

        [Tooltip("이 레이어들에 가려져도 무시하고 그 위에 외곽선을 그립니다. (예: 물에 잠겨도 선을 그리고 싶다면 Water 체크)")]
        public LayerMask forceEdgeMask = 0;
    }

    #endregion

    [CreateAssetMenu(fileName = "NewMapBakeSettings", menuName = "CoreEngine/LevelDesign/Map Bake Settings")]
    public class MapBakeSettingsSO : ScriptableObject
    {
        // ---------------------------------------------------------
        // 파일 저장 경로 -> 지금의 ui 유지
        // ---------------------------------------------------------
        public string saveDirectory;

        // ---------------------------------------------------------
        // 에디터 핸들 -> 지금의 ui 유지
        // ---------------------------------------------------------
        public bool showInteractiveGizmo = true; 
        public bool showCameraGizmo = true;

        // ---------------------------------------------------------
        // 카메라 설정
        // ---------------------------------------------------------
        // [SerializeField, HideInInspector] private MapDimension _lastMapDimension; // 굳이 필요없는듯
        // public MapDimension mapDimension = MapDimension._3D; // 굳이 필요없는듯
        public MapProjectionPlane projectionPlane = MapProjectionPlane.XZ;
        public Vector3 centerPosition;
        public Vector2 totalMapSize = new Vector2(1024, 1024);
        public Vector2 tileSize = new Vector2(720, 480);
        public float captureOffset = 500f;
        public float maxDepth = 1000f;
        

        // ---------------------------------------------------------
        // 렌더링 및 캡처 설정
        // ---------------------------------------------------------
        public MapResolution resolution = MapResolution.Res_512;
        public LayerMask renderMask = -1;
        public Color backgroundColor = Color.black;
        public Color mapTintColor = Color.white;
        public bool useLayerColor;

        // ---------------------------------------------------------
        // 명도 양자화 설정
        // ---------------------------------------------------------
        public MapDepthSteps depthSteps = MapDepthSteps.Step_8;
        [Range(0, 1)] public float finalDepthBrightness = 0.5f;
        public LayerMask ignoreDepthQuantizationMask = 0;

        // ---------------------------------------------------------
        // 4. 고정 길이 데이터 (32 Layers) - 에디터 자동 동기화 -> 지금의 ui 유지
        // ---------------------------------------------------------
        [HideInInspector] public List<LayerColorPair> layerColors = new List<LayerColorPair>();
        [HideInInspector] public List<LayerOutlineSetting> outlineSettings = new List<LayerOutlineSetting>();

        // 그리드 분할 계산 프로퍼티
        public int Cols => tileSize.x > 0 ? Mathf.CeilToInt(totalMapSize.x / tileSize.x) : 1;
        public int Rows => tileSize.y > 0 ? Mathf.CeilToInt(totalMapSize.y / tileSize.y) : 1;

        //private void Awake()
        //{
        //    _lastMapDimension = mapDimension;
        //}

#if UNITY_EDITOR
        private void OnValidate()
        {
            //ApplyDimensionPreset();
            PreventAlphaZeroInOutlines();
        }

        //private void ApplyDimensionPreset()
        //{
        //    if (mapDimension != _lastMapDimension)
        //    {
        //        _lastMapDimension = mapDimension;
        //        if (mapDimension == MapDimension._2D)
        //        {
        //            projectionPlane = MapProjectionPlane.XY;
        //            depthSteps = MapDepthSteps.None;
        //            useLayerColor = false;
        //        }
        //        else if (mapDimension == MapDimension._3D)
        //        {
        //            projectionPlane = MapProjectionPlane.XZ;
        //            depthSteps = MapDepthSteps.Step_8;
        //            useLayerColor = true;
        //        }
        //    }
        //}

        private void PreventAlphaZeroInOutlines()
        {
            if (outlineSettings == null) return;
            for (int i = 0; i < outlineSettings.Count; i++)
            {
                if (outlineSettings[i].outlineColor.a <= 0.01f)
                {
                    Color correctedColor = outlineSettings[i].outlineColor;
                    correctedColor.a = 1f;
                    outlineSettings[i].outlineColor = correctedColor;
                }
            }
        }
#endif
    }
}