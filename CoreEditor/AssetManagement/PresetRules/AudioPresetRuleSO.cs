using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    [CreateAssetMenu(fileName = "AudioOverrideRule", menuName = "CoreEngine/Tools/Rules/Audio Import Rule")]
    public class AudioPresetRuleSO : PresetOverrideRuleSO
    {
        // 🌟 유니티 오디오 프리셋 타입명("AudioImporter")과 일치하도록 설정
        public override string TargetTypeName => nameof(AudioImporter);

        [Header("보존할 오디오 속성 (체크 시 원본 값 유지)")]

        [Tooltip("모노 채널 강제 변환(Force To Mono) 설정을 유지합니다.")]
        public bool keepForceToMono = true;

        [Tooltip("백그라운드 로드(Load In Background) 설정을 유지합니다.")]
        public bool keepLoadInBackground = false;

        [Tooltip("앰비소닉(Ambisonic) 오디오 설정을 유지합니다.")]
        public bool keepAmbisonic = false;

        [Space(10)]
        [Tooltip("기본 샘플 세팅(Load Type, Compression Format, Quality 등)을 통째로 유지합니다.")]
        public bool keepDefaultSampleSettings = false;

        public override bool TryApplyWithRule(AssetImporter baseImporter, Preset preset)
        {
            // 1. 강타입 캐스팅 검사 (오디오 임포터가 아니면 패스)
            if (baseImporter is not AudioImporter audioImporter)
                return false;

            // 2. 프리셋 호환성 검사
            if (!preset.CanBeAppliedTo(audioImporter))
                return false;

            // ==========================================
            // 🌟 3. Cache (원본 데이터 백업)
            // ==========================================
            bool cachedForceToMono = audioImporter.forceToMono;
            bool cachedLoadInBackground = audioImporter.loadInBackground;
            bool cachedAmbisonic = audioImporter.ambisonic;

            // 핵심: 압축 방식, 로드 타입(DecompressOnLoad 등)이 담긴 구조체를 통째로 캐싱
            AudioImporterSampleSettings cachedSampleSettings = audioImporter.defaultSampleSettings;

            // ==========================================
            // 🌟 4. Apply (프리셋 통째로 바르기)
            // ==========================================
            preset.ApplyTo(audioImporter);

            // ==========================================
            // 🌟 5. Restore (조건에 따라 원본 복구)
            // ==========================================
            if (keepForceToMono)
                audioImporter.forceToMono = cachedForceToMono;

            if (keepLoadInBackground)
                audioImporter.loadInBackground = cachedLoadInBackground;

            if (keepAmbisonic)
                audioImporter.ambisonic = cachedAmbisonic;

            if (keepDefaultSampleSettings)
                audioImporter.defaultSampleSettings = cachedSampleSettings;

            return true; // 성공적으로 덮어씀
        }
    }
}