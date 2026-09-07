using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.StaticData
{
    public interface IAssetRegistry<TRegistry, TAsset>
        where TRegistry : IAssetRegistry<TRegistry, TAsset>
        where TAsset : UnityEngine.Object
    {
        TRegistry Instance { get; }
    }
    public abstract class AssetRegistry<TRegistry, TAsset> : ScriptableObject, IAssetRegistry<TRegistry, TAsset>
        where TRegistry : AssetRegistry<TRegistry, TAsset>
        where TAsset : UnityEngine.Object
    {
        private static TRegistry _inst;
        public static TRegistry Inst => _inst;
        // 인스턴스 인터페이스 구현: 정적 _inst를 인스턴스 프로퍼티로 노출
        public TRegistry Instance => _inst;

        protected virtual void Awake()
        {
            if (_inst != null && _inst != this)
            {
                Debug.LogWarning($"[AssetRegistry] 중복된 {typeof(TRegistry).Name} 레지스트리가 로드되었습니다. 기존 객체를 유지하고 새 객체를 파괴합니다.");
#if UNITY_EDITOR
                // 이미 존재하는 원본 레지스트리를 인스펙터에서 하이라이트(핑) 처리
                EditorGUIUtility.PingObject(_inst);
#endif
                DestroyImmediate(this);
                return;
            }

            _inst = (TRegistry)this;
        }

        // 실제 하위 클래스(예: SpriteRegistry)에서 Addressable ResourceManager 인프라를 활용하여 구현할 부분
        public static TAsset Get(int id)
        {
            return null;
        }
    }
}