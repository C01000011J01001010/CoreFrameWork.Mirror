using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.EditorTools
{
    public class FontReplacementWindow : EditorWindow
    {
        private TMP_FontAsset targetFont;

        private enum SearchScope
        {
            EntireScene,
            SelectedGameObject
        }

        private SearchScope searchScope = SearchScope.EntireScene;
        private bool includeChildren = true;
        private bool includeInactive = true;
        private bool skipSameFont = true;

        private bool includeTextMeshProUGUI = true;
        private bool includeTextMeshPro3D = true;

        private readonly List<TMP_Text> results = new();

        private Vector2 scrollPosition;

        [MenuItem(Constants.ToolRootUi+"Font Replacement")]
        public static void Open()
        {
            GetWindow<FontReplacementWindow>("Font Replacement");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Target Font", EditorStyles.boldLabel);
            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                targetFont,
                typeof(TMP_FontAsset),
                false
            );

            EditorGUILayout.Space(12);

            EditorGUILayout.LabelField("Search Scope", EditorStyles.boldLabel);

            searchScope = (SearchScope)EditorGUILayout.EnumPopup(
                "Scope",
                searchScope
            );

            if (searchScope == SearchScope.SelectedGameObject)
            {
                includeChildren = EditorGUILayout.ToggleLeft(
                    "Include Children",
                    includeChildren
                );

                if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.HelpBox(
                        "Select a GameObject in the Hierarchy.",
                        MessageType.Warning
                    );
                }
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Target Components", EditorStyles.boldLabel);

            includeTextMeshProUGUI = EditorGUILayout.ToggleLeft(
                "TextMeshProUGUI",
                includeTextMeshProUGUI
            );

            includeTextMeshPro3D = EditorGUILayout.ToggleLeft(
                "TextMeshPro (3D)",
                includeTextMeshPro3D
            );

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            includeInactive = EditorGUILayout.ToggleLeft(
                "Include Inactive Objects",
                includeInactive
            );

            skipSameFont = EditorGUILayout.ToggleLeft(
                "Skip Objects Using Target Font",
                skipSameFont
            );

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(targetFont == null))
            {
                if (GUILayout.Button("Scan", GUILayout.Height(30)))
                {
                    Scan();
                }
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField(
                $"Found: {results.Count}",
                EditorStyles.boldLabel
            );

            DrawResults();

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(
                targetFont == null || results.Count == 0))
            {
                if (GUILayout.Button("Replace Font", GUILayout.Height(35)))
                {
                    ReplaceFont();
                }
            }
        }

        private void Scan()
        {
            results.Clear();

            if (searchScope == SearchScope.SelectedGameObject)
            {
                if (Selection.activeGameObject == null)
                    return;

                ScanGameObject(Selection.activeGameObject);
            }
            else
            {
                foreach (GameObject root in UnityEngine.SceneManagement.SceneManager
                             .GetActiveScene()
                             .GetRootGameObjects())
                {
                    ScanGameObject(root);
                }
            }

            Repaint();
        }

        private void ScanGameObject(GameObject root)
        {
            TMP_Text[] texts = includeChildren
                ? root.GetComponentsInChildren<TMP_Text>(includeInactive)
                : root.GetComponents<TMP_Text>();

            foreach (TMP_Text text in texts)
            {
                if (!IsTargetComponent(text))
                    continue;

                if (skipSameFont && text.font == targetFont)
                    continue;

                results.Add(text);
            }
        }

        private bool IsTargetComponent(TMP_Text text)
        {
            if (text is TextMeshProUGUI)
                return includeTextMeshProUGUI;

            if (text is TextMeshPro)
                return includeTextMeshPro3D;

            return false;
        }

        private void DrawResults()
        {
            if (results.Count == 0)
                return;

            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.MinHeight(150)
            );

            foreach (TMP_Text text in results)
            {
                if (text == null)
                    continue;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.ObjectField(
                    text,
                    typeof(TMP_Text),
                    true
                );

                string type = text is TextMeshProUGUI
                    ? "UI"
                    : "3D";

                GUILayout.Label(
                    type,
                    GUILayout.Width(30)
                );

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ReplaceFont()
        {
            if (targetFont == null)
                return;

            int changed = 0;

            Undo.SetCurrentGroupName("Replace TMP Fonts");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (TMP_Text text in results)
            {
                if (text == null)
                    continue;

                if (skipSameFont && text.font == targetFont)
                    continue;

                Undo.RecordObject(text, "Replace TMP Font");

                text.font = targetFont;

                EditorUtility.SetDirty(text);

                changed++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"[Font Replacement] Changed {changed} TMP text components."
            );

            Scan();
        }
    }
}

