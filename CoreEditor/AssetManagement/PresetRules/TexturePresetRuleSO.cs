using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace CoreEngine.EditorTools
{
    [CreateAssetMenu(fileName = "TextureOverrideRule", menuName = "CoreEngine/Tools/Rules/Texture Import Rule")]
    public class TexturePresetRuleSO : PresetOverrideRuleSO
    {
        // 🌟 유니티가 텍스처 프리셋에 부여하는 타입명("TextureImporter")과 일치하도록 설정
        public override string TargetTypeName => nameof(TextureImporter);

        [Header("보존할 텍스처 속성 (체크 시 원본 값 유지)")]
        public bool keepMaxTextureSize = true;
        public bool keepTextureCompression = true;
        public bool keepTextureType = false;
        public bool keepFilterMode = false;
        public bool keepWrapMode = false;

        public override bool TryApplyWithRule(AssetImporter baseImporter, Preset preset)
        {
            // 1. 강타입 캐스팅 검사 (텍스처 임포터가 아니면 패스)
            if (baseImporter is not TextureImporter texImporter)
                return false;

            // 2. 프리셋 적용 가능 여부 검사
            if (!preset.CanBeAppliedTo(texImporter))
                return false;

            // ==========================================
            // Cache (원본 데이터 백업)
            // ==========================================
            int cachedMaxSize = texImporter.maxTextureSize;
            TextureImporterCompression cachedCompression = texImporter.textureCompression;
            TextureImporterType cachedType = texImporter.textureType;
            FilterMode cachedFilterMode = texImporter.filterMode;
            TextureWrapMode cachedWrapMode = texImporter.wrapMode;

            // ==========================================
            // Apply (프리셋 통째로 바르기)
            // ==========================================
            preset.ApplyTo(texImporter);

            // ==========================================
            // Restore (조건에 따라 원본 복구)
            // ==========================================
            if (keepMaxTextureSize) texImporter.maxTextureSize = cachedMaxSize;
            if (keepTextureCompression) texImporter.textureCompression = cachedCompression;
            if (keepTextureType) texImporter.textureType = cachedType;
            if (keepFilterMode) texImporter.filterMode = cachedFilterMode;
            if (keepWrapMode) texImporter.wrapMode = cachedWrapMode;

            return true;
        }
    }
}