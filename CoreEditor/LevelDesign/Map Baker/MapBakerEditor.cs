using UnityEditor;
using UnityEngine;
using System.IO;

namespace CoreEngine.LevelDesign.Editor
{
    [CustomEditor(typeof(MapBaker))]
    public class MapBakerEditor : UnityEditor.Editor
    {
        private SerializedObject _profileSO;

        private MapBaker _baker;

        private void OnEnable()
        {
            _baker = (MapBaker)target;
        }



        public override void OnInspectorGUI()
        {
            // 🌟 공용 UI 호출 (생성 시 바인딩될 동작을 람다식으로 전달)
            MapBakeEditorUI.DrawDefaultGUI(serializedObject, ref _profileSO, ref _baker.settingsProfile, (newSettings) =>
            {
                _baker.settingsProfile = newSettings;
                EditorUtility.SetDirty(_baker);
            });

            if (_baker.settingsProfile == null) return;

            MapBakeEditorUI.DrawSharedGUI(_profileSO, _baker.settingsProfile);
        }

        private void OnSceneGUI()
        {
            if (_baker.settingsProfile == null) return;

            MapBakeGizmoDrawer.DrawWithHandles(_baker.settingsProfile);
        }
    }
}
