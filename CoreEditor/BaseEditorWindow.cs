using CoreEditor.Extensions;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    public abstract class BaseEditorWindow<EditorSetting> : EditorWindow
    where EditorSetting : ScriptableObject
    {
        protected EditorSetting editorSetting;
        protected void LoadSavedSettings(string fileName)
        {
            editorSetting = this.LoadSavedEditorSetting<EditorSetting>(fileName);
        }

        protected void UpdateSetting()
        {
            EditorUtility.SetDirty(editorSetting);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

}
