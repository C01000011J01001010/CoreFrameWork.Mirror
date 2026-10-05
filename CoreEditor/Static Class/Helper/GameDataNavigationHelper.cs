using System;
using UnityEditor;
using UnityEngine;
using CoreEditor.GameData;

namespace CoreEditor.Helpers
{
    public static class GameDataNavigationHelper
    {
        public static Vector2 TapSize = new Vector2(TapHeight, TapWidth);
        public const float TapHeight = 700f;
        public const float TapWidth = 500f;
        public enum Tab
        {
            Organizer = 1,
            PreloadSetter = 2,
            CsvProcessor = 3,
            IntegrityChecker = 4
        }

        public static void DrawTopNavigationBar(Tab currentTab)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawTabButton(1, GameDataOrganizer.WindowName, currentTab == Tab.Organizer,
                () => EditorWindow.GetWindow<GameDataOrganizer>(GameDataOrganizer.WindowName).Show());

            DrawTabButton(2, PreloadAddressableSetter.WindowName, currentTab == Tab.PreloadSetter,
                () => EditorWindow.GetWindow<PreloadAddressableSetter>(PreloadAddressableSetter.WindowName).Show());

            DrawTabButton(3, CsvToTableBatchProcessor.WindowName, currentTab == Tab.CsvProcessor,
                () => EditorWindow.GetWindow<CsvToTableBatchProcessor>(CsvToTableBatchProcessor.WindowName).Show());

            DrawTabButton(4, TableIntegrityChecker.WindowName, currentTab == Tab.IntegrityChecker,
                () => EditorWindow.GetWindow<TableIntegrityChecker>(TableIntegrityChecker.WindowName).Show());

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTabButton(int index, string windowName, bool isSelected, Action onClick)
        {
            // 선택된 탭이면 시안색 적용
            if (isSelected) GUI.backgroundColor = Color.cyan;

            // 버튼 렌더링 및 클릭 이벤트 처리 (현재 탭이 아닐 때만 onClick 실행)
            if (GUILayout.Button($"{index}. {windowName}", EditorStyles.toolbarButton) && !isSelected)
            {
                onClick?.Invoke();
            }

            // 색상 원상복구
            if (isSelected) GUI.backgroundColor = Color.white;
        }
    }
}