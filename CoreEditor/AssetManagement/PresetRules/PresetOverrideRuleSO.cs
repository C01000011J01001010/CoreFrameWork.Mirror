using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    /// <summary>
    /// 에셋 프리셋 적용 시 캐싱 및 복구(Cache & Restore) 전략을 정의하는 추상 SO
    /// </summary>
    public abstract class PresetOverrideRuleSO : ScriptableObject
    {
        /// <summary>
        /// 이 룰이 다루는 Target Importer의 타입 이름 (예: "TextureImporter", "AudioImporter")
        /// 유니티 Preset.GetTargetTypeName()과 비교하는 식별자로 사용됩니다.
        /// </summary>
        public abstract string TargetTypeName { get; }

        /// <summary>
        /// 주입된 Preset의 타입과 이 룰 SO의 타겟 타입이 일치하는지 검사
        /// </summary>
        public virtual bool IsCompatibleWithPreset(Preset preset)
        {
            if (preset == null) return false;
            // 유니티 Preset API를 통해 타입 문자열을 가져와 룰의 타겟 타입과 비교
            return preset.GetTargetTypeName() == TargetTypeName;
        }

        /// <summary>
        /// 전달받은 Importer에 Preset을 규칙에 맞춰 주입합니다.
        /// </summary>
        public abstract bool TryApplyWithRule(AssetImporter baseImporter, Preset preset);
    }
}