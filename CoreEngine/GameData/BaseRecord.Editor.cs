#if UNITY_EDITOR
using CoreEngine.Helpers;
using UnityEngine;

namespace CoreEngine.GameData
{
    public interface IReferenceKey : IIdentifiable
    {
        string GetEditorRawKey();
    }
    internal interface IBakeId
    {
        ulong BakeID();
    }

    public abstract partial class BaseRecord : IRecord, IBakeId
    {
        internal const string KeyException = "[INVALID_REFERENCE_KEY_IN_PK_DETECTED]";

        [SerializeField]
        private string _primarykey;


        ulong IBakeId.BakeID()
        {
            string pk = GetPrimaryKey();

            if (string.IsNullOrWhiteSpace(pk))
            {
                LogHelper.LogError($"[{nameof(BaseRecord)}] Invalid PK Error: {this.GetType().Name}의 PrimaryKey가 비어있습니다.");
                _hashCode = 0;
                return _hashCode;
            }
            if (pk.Contains(KeyException))
            {
                LogHelper.LogError($"{pk}은 PK에 포함될 수 없는 값({KeyException})을 가졌습니다.");
                _hashCode = 0;
                return _hashCode;
            }


            _primarykey = pk;

            // 단일 문자열을 해싱하여 ID로 확정
            _hashCode = HashHelper.StringToId(pk);
            return _hashCode;
        }
    }

}
#endif
