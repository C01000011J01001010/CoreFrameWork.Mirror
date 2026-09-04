#if UNITY_EDITOR
using CoreEngine.LevelDesign;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.LevelDesign
{
    public class MapBakerWindow : EditorWindow
    {
        public MapBakeSettingsSO settingsProfile;

        private SerializedObject _windowSO;
        private SerializedObject _profileSO;
        private Vector2 _scrollPos;

        [MenuItem(Constants.ToolRootLevelDesgin+"Map Baker")]
        public static void ShowWindow() => GetWindow<MapBakerWindow>("Map Baker");

        private void OnEnable()
        {
            _windowSO = new SerializedObject(this);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            // 🌟 윈도우 역시 동일한 공용 UI 호출
            MapBakeEditorUI.DrawDefaultGUI(_windowSO, ref _profileSO, ref settingsProfile, (newSettings) =>
            {
                settingsProfile = newSettings;
                _windowSO.ApplyModifiedProperties();
            });

            if (settingsProfile == null) return;


            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            MapBakeEditorUI.DrawSharedGUI(_profileSO, settingsProfile);
            EditorGUILayout.EndScrollView();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (settingsProfile == null) return;

            // 🌟 윈도우는 반드시 Handles API 버전을 호출합니다.
            MapBakeGizmoDrawer.DrawWithHandles(settingsProfile);

            sceneView.Repaint();
        }
    }
}
#endif