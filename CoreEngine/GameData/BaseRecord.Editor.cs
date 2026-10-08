#if UNITY_EDITOR
using CoreEngine.Helpers;
using UnityEngine;

namespace CoreEngine.GameData
{
    public interface IReferenceKey : IIdentifiable
    {
        string GetEditorRawKey();
    }
    internal interface IEditorRecordSetup
    {
        void EditorSetupId(ulong id, string primaryKey);
    }

    public abstract partial class BaseRecord : IEditorRecordSetup
    {
        internal const string KeyException = "[INVALID_REFERENCE_KEY_IN_PK_DETECTED]";

        [SerializeField]
        private string _primarykey;

        void IEditorRecordSetup.EditorSetupId(ulong id, string primaryKey)
        {
            _hashCode = id;
            _primarykey = primaryKey;
        }
    }

}
#endif
