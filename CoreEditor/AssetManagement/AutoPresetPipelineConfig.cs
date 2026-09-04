//using System;
//using System.Collections.Generic;
//using System.IO;
//using UnityEditor;
//using UnityEditor.Presets;
//using UnityEngine;

//namespace CoreEngine.EditorTools
//{
//    // 🌟 [CreateAssetMenu] 삭제: 유저가 우클릭으로 여러 개 만드는 것을 원천 차단합니다!
//    public class AutoPresetPipelineConfig : ScriptableObject
//    {
//        [Serializable]
//        public class AutoRule
//        {
//            [Tooltip("적용할 폴더 경로 (예: Assets/UI/Icons)")]
//            public string targetFolderPath;

//            [Tooltip("자동으로 덮어씌울 프리셋 원본")]
//            public Preset targetPreset;

//            [Tooltip("프리셋 적용 조건 및 캐싱 룰")]
//            public PresetOverrideRuleSO activeRule;
//        }

//        [Header("자동 프리셋 파이프라인 규칙 리스트")]
//        public List<AutoRule> rules = new List<AutoRule>();

//        // ==========================================
//        // 🌟 싱글톤 에셋 관리 로직 (Singleton Asset)
//        // ==========================================

//        /// <summary>
//        /// 상단 메뉴를 통해 설정 파일에 안전하게 접근합니다.
//        /// </summary>
//        [MenuItem("Tools/Core System/Auto Preset Pipeline Settings")]
//        public static void SelectSettings()
//        {
//            var config = GetOrCreateSettings();
//            Selection.activeObject = config; // 인스펙터 창에 바로 띄우기
//            EditorGUIUtility.PingObject(config); // 프로젝트 창에서 파일 위치를 반짝이며 강조
//        }

//        /// <summary>
//        /// 설정 파일이 프로젝트에 있으면 가져오고, 없으면 지정된 경로에 자동으로 단 1개만 생성합니다.
//        /// </summary>
//        public static AutoPresetPipelineConfig GetOrCreateSettings()
//        {
//            // 프로젝트 내의 해당 타입 에셋을 모두 검색
//            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AutoPresetPipelineConfig)}");

//            if (guids.Length > 0)
//            {
//                if (guids.Length > 1)
//                {
//                    Debug.LogWarning("[Auto Preset Pipeline] 설정 파일이 2개 이상 발견되었습니다! 파편화를 막기 위해 하나만 유지해주세요.");
//                }
//                return AssetDatabase.LoadAssetAtPath<AutoPresetPipelineConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
//            }

//            // 파일이 없다면 코어 폴더 하위에 안전하게 자동 생성
//            var config = CreateInstance<AutoPresetPipelineConfig>();

//            // 원하는 저장 경로를 지정하세요. (폴더가 없으면 자동 생성됨)
//            string directory = "Assets/CoreEngine/Settings";
//            if (!Directory.Exists(directory))
//            {
//                Directory.CreateDirectory(directory);
//            }

//            string assetPath = $"{directory}/AutoPresetPipelineConfig.asset";
//            AssetDatabase.CreateAsset(config, assetPath);
//            AssetDatabase.SaveAssets();
//            AssetDatabase.Refresh();

//            Debug.Log($"[Auto Preset Pipeline] 파이프라인 설정 파일이 새로 생성되었습니다: {assetPath}");

//            return config;
//        }
//    }
//}