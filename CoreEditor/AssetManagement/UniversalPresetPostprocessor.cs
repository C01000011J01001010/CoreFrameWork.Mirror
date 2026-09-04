//using UnityEditor;
//using UnityEngine;

//namespace CoreEngine.EditorTools
//{
//    /// <summary>
//    /// 프로젝트에 에셋이 들어오는 순간을 가로채서 규칙에 맞게 프리셋을 발라주는 백그라운드 파이프라인
//    /// </summary>
//    public class UniversalPresetPostprocessor : AssetPostprocessor
//    {
//        private static AutoPresetPipelineConfig _cachedRuleSO;

//        // 이 함수는 어떤 에셋이든 유니티에 Import 되기 직전(메타데이터 생성 전)에 무조건 호출됩니다.
//        private void OnPreprocessAsset()
//        {
//            // 1. 규칙 파일 로드 (최초 1회만 검색하여 캐싱)
//            if (_cachedRuleSO == null)
//            {
//                string[] guids = AssetDatabase.FindAssets("t:AutoPresetRuleSO");
//                if (guids.Length > 0)
//                {
//                    _cachedRuleSO = AssetDatabase.LoadAssetAtPath<AutoPresetPipelineConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
//                }
//                else
//                {
//                    return; // 프로젝트에 규칙 SO가 없으면 아무 일도 하지 않음
//                }
//            }

//            // 2. 현재 들어온 에셋의 경로(assetPath)가 설정된 규칙에 포함되는지 검사
//            foreach (var rule in _cachedRuleSO.rules)
//            {
//                // 경로, 프리셋, 룰이 모두 정상적으로 세팅되어 있고, 현재 에셋 경로가 타겟 경로를 포함한다면
//                if (!string.IsNullOrEmpty(rule.targetFolderPath) && assetPath.Contains(rule.targetFolderPath))
//                {
//                    if (rule.targetPreset != null && rule.activeRule != null && assetImporter != null)
//                    {
//                        // 3. 타입 안정성 검증 후 자동 적용 (우리가 만든 똑똑한 Rule SO를 통해 실행!)
//                        if (rule.activeRule.TryApplyWithRule(assetImporter, rule.targetPreset))
//                        {
//                            // 🌟 OnPreprocessAsset 단계에서는 SaveAndReimport()를 호출하면 안 됩니다! 
//                            // (유니티가 임포트하는 과정 중이므로, 이 함수가 끝나면 유니티가 알아서 저장합니다)
//                            Debug.Log($"[Auto Preset] {assetPath} 에 자동으로 프리셋이 적용되었습니다.");
//                            break; // 하나의 에셋에 규칙이 적용되면 루프 종료
//                        }
//                    }
//                }
//            }
//        }
//    }
//}