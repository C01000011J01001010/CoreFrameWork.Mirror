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
        [SerializeField]
        private string _primarykey;

        void IEditorRecordSetup.EditorSetupId(ulong id, string primaryKey)
        {
            _hash = id;
            _primarykey = primaryKey;
        }
    }

}
#endif
