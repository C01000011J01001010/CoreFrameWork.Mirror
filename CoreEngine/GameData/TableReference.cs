using System;
using UnityEngine;

namespace CoreEngine.GameData
{
    /// <summary>
    /// 에디터에서는 _DataTable 에셋 할당을 지원하고,
    /// 런타임에서는 리플렉션 안전성을 위해 AssemblyQualifiedName 기반의 Type 객체로 변환됩니다.
    /// </summary>
    [Serializable]
    public class TableReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private _DataTable tableAsset;
#endif

        [SerializeField, HideInInspector] private string tableFullName = string.Empty;

        // 런타임에서 호출할 프로퍼티: 문자열을 Type 객체로 변환하여 반환
        public Type TableType => !string.IsNullOrEmpty(tableFullName) ? Type.GetType(tableFullName) : null;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (tableAsset != null)
            {
                // 다중 어셈블리(asmdef) 환경에서도 완벽히 타입을 찾기 위해 AssemblyQualifiedName 사용
                tableFullName = tableAsset.GetType().AssemblyQualifiedName;
            }
            else
            {
                tableFullName = string.Empty;
            }
#endif
        }

        public void OnAfterDeserialize() { }

        // TableReference -> Type 매칭
        //public static implicit operator Type(TableReference reference)
        //{
        //    return reference.TableType;
        //}
    }
}