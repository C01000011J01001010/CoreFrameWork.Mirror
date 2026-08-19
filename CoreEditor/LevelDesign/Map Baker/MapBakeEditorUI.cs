#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using CoreEditor; // 🌟 갱신된 범용 에디터 유틸리티 네임스페이스 참조

namespace CoreEngine.LevelDesign.Editor
{
    public static class MapBakeEditorUI
    {
        #region [1] 최상위 UI 진입점 (Main Entry Points)

        public static void DrawDefaultGUI(SerializedObject viewObject, ref SerializedObject profileSO, ref MapBakeSettingsSO settings, Action<MapBakeSettingsSO> onProfileCreated)
        {
            viewObject.Update();

            SerializedProperty profileProp = viewObject.FindProperty("settingsProfile");

            // 🌟 만능 프로필 생성기 (CoreEditorUtility) 연동
            UtilitySettingData.DrawProfileSetupGUI<MapBakeSettingsSO>(
                profileProp,
                "할당된 세팅 프로필 에셋이 없습니다. 새로 생성하시겠습니까?",
                "MapBakeSettings",
                (newSettings, savePath) =>
                {
                    // 생성 직후: MapBaker만의 고유한 세팅 (저장 폴더 경로 기록)
                    string dirPath = System.IO.Path.GetDirectoryName(savePath).Replace("\\", "/");
                    newSettings.saveDirectory = dirPath;

                    // 외부 콜백 실행 (인스펙터 갱신 등)
                    onProfileCreated?.Invoke(newSettings);
                }
            );

            viewObject.ApplyModifiedProperties();

            // 최신 세팅 값 동기화
            settings = profileProp.objectReferenceValue as MapBakeSettingsSO;

            if (settings != null)
            {
                if (profileSO == null || profileSO.targetObject != settings)
                {
                    profileSO = new SerializedObject(settings);
                }
            }
        }

        public static void DrawSharedGUI(SerializedObject profileSO, MapBakeSettingsSO settings)
        {
            if (profileSO == null || settings == null) return;

            SyncAllLayersFixed(settings); // 32개 레이어 데이터 동기화 유지

            profileSO.Update();

            DrawSaveDirectoryGUI(profileSO, settings);
            DrawGeneralSettingsGUI(profileSO);
            DrawRenderSettingsGUI(profileSO, settings);
            DrawOutlineSettingsGUI(profileSO, settings);

            profileSO.ApplyModifiedProperties();

            DrawBakeActionGUI(settings);
        }

        #endregion

        #region [2] 세부 UI 드로잉 영역 (Sub UI Methods)

        private static void DrawSaveDirectoryGUI(SerializedObject profileSO, MapBakeSettingsSO settings)
        {
            DrawSectionHeader("📁 파일 저장 경로");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📂 저장 폴더 선택", GUILayout.Height(26)))
            {
                string defaultPath = string.IsNullOrEmpty(settings.saveDirectory) ? Application.dataPath : settings.saveDirectory;
                string absPath = EditorUtility.OpenFolderPanel("저장 위치 선택", defaultPath, "");

                if (!string.IsNullOrEmpty(absPath))
                {
                    if (absPath.StartsWith(Application.dataPath))
                    {
                        profileSO.FindProperty("saveDirectory").stringValue = "Assets" + absPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("경로 오류", "프로젝트 내부(Assets 폴더 하위)를 선택해야 합니다.", "확인");
                    }
                }
            }

            if (GUILayout.Button("🔍 위치 확인 (Ping)", GUILayout.Height(26)))
            {
                if (!string.IsNullOrEmpty(settings.saveDirectory))
                {
                    UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(settings.saveDirectory);
                    if (folderObj != null) EditorGUIUtility.PingObject(folderObj);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(profileSO.FindProperty("saveDirectory"), new GUIContent("현재 저장 경로"));
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawGeneralSettingsGUI(SerializedObject profileSO)
        {
            DrawSectionHeader("⚙️ 에디터 핸들 및 뷰 설정");
            EditorGUILayout.PropertyField(profileSO.FindProperty("showInteractiveGizmo"), new GUIContent("씬 뷰 화살표 핸들 켜기"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("showCameraGizmo"), new GUIContent("씬 뷰 카메라 방향 켜기"));

            DrawSectionHeader("📷 카메라 및 영역 설정");
            EditorGUILayout.PropertyField(profileSO.FindProperty("projectionPlane"), new GUIContent("투영 평면"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("centerPosition"), new GUIContent("월드 중심 좌표"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("totalMapSize"), new GUIContent("전체 맵 크기"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("tileSize"), new GUIContent("타일 분할 크기"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("captureOffset"), new GUIContent("카메라 렌더 깊이 (Offset)"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("maxDepth"), new GUIContent("최대 캡처 깊이 (Far)"));
        }

        private static void DrawRenderSettingsGUI(SerializedObject profileSO, MapBakeSettingsSO settings)
        {
            DrawSectionHeader("🎨 렌더링 및 베이스 색상");
            EditorGUILayout.PropertyField(profileSO.FindProperty("resolution"), new GUIContent("타일 해상도"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("renderMask"), new GUIContent("렌더링 마스크"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("backgroundColor"), new GUIContent("배경 색상"));
            EditorGUILayout.PropertyField(profileSO.FindProperty("mapTintColor"), new GUIContent("맵 전체 테마 색상 (Tint)"));

            DrawSectionHeader("🏔️ 명도 양자화 (등고선) 설정");
            EditorGUILayout.PropertyField(profileSO.FindProperty("depthSteps"), new GUIContent("명도 양자화 단계"));

            if (settings.depthSteps >= MapDepthSteps.Step_2)
            {
                EditorGUILayout.PropertyField(profileSO.FindProperty("finalDepthBrightness"), new GUIContent("가장 깊은 곳 밝기 제한"));
                EditorGUILayout.PropertyField(profileSO.FindProperty("ignoreDepthQuantizationMask"), new GUIContent("양자화 예외 대상 (Layer)"));
            }

            DrawSectionHeader("🌈 레이어별 색상 오버라이드");
            if (settings.depthSteps != MapDepthSteps.None)
            {
                SerializedProperty useLayerColorProp = profileSO.FindProperty("useLayerColor");
                EditorGUILayout.PropertyField(useLayerColorProp, new GUIContent("레이어별 고유 색상 사용"));

                if (useLayerColorProp.boolValue)
                {
                    EditorGUILayout.HelpBox("렌더링 레이어를 지정 색상으로 오버라이드 합니다.", MessageType.Info);
                    DrawLayerColorPalette(profileSO.FindProperty("layerColors"), settings);
                }
                else
                {
                    EditorGUILayout.HelpBox("레이어 구별 없이 흑백모드를 사용중입니다." +
                        "\nTint 설정을 확인해주세요.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("현재 명도 양자화가 'None' 상태입니다." +
                    "\n씬의 원본 머티리얼과 텍스처를 그대로 캡처합니다.", MessageType.Info);
            }
        }

        private static void DrawOutlineSettingsGUI(SerializedObject profileSO, MapBakeSettingsSO settings)
        {
            DrawSectionHeader("🖌️ 외곽선 설정");
            EditorGUILayout.HelpBox("체크된 특정 레이어의 테두리에만 외곽선을 렌더링합니다.", MessageType.Info);


            SerializedProperty outlineSettingsProp = profileSO.FindProperty("outlineSettings");
            bool isFirst = true;

            IterateActiveLayers(settings, outlineSettingsProp, (index, itemProp) =>
            {
                if (!isFirst) DrawSeparator();

                EditorGUILayout.PropertyField(itemProp, true);
                isFirst = false;
            });
        }

        private static void DrawLayerColorPalette(SerializedProperty layerColorsProp, MapBakeSettingsSO settings)
        {
            EditorGUI.indentLevel++;
            IterateActiveLayers(settings, layerColorsProp, (index, pairProp) =>
            {
                SerializedProperty nameProp = pairProp.FindPropertyRelative("layerName");
                SerializedProperty colorProp = pairProp.FindPropertyRelative("color");
                EditorGUILayout.PropertyField(colorProp, new GUIContent(nameProp.stringValue));
            });
            EditorGUI.indentLevel--;
        }

        private static void DrawBakeActionGUI(MapBakeSettingsSO settings)
        {
            DrawSeparator();
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox($"총 생성될 타일 개수: {settings.Cols} x {settings.Rows} = {settings.Cols * settings.Rows}개", MessageType.Info);

            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("🚀 전체 그리드 맵 굽기", GUILayout.Height(40)))
            {
                BakeGridMap(settings);
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region [3] UI Custom Rendering Helpers (디자인 유틸리티)

        private static void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1f));
            EditorGUILayout.Space(5);
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(5);
        }

        private static void IterateActiveLayers(MapBakeSettingsSO settings, SerializedProperty arrayProp, Action<int, SerializedProperty> onDrawItem)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((settings.renderMask.value & (1 << i)) != 0 && !string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                {
                    if (i < arrayProp.arraySize)
                    {
                        onDrawItem?.Invoke(i, arrayProp.GetArrayElementAtIndex(i));
                    }
                }
            }
        }

        #endregion

        #region [4] 베이킹 프로세스 및 파일 입출력 (Bake Execution & IO)

        private static void BakeGridMap(MapBakeSettingsSO settings)
        {
            if (string.IsNullOrEmpty(settings.saveDirectory))
            {
                EditorUtility.DisplayDialog("저장 실패", "저장 경로가 비어있습니다. '찾기' 버튼을 눌러 경로를 지정해주세요.", "확인");
                return;
            }

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "UntitledScene";

            string dirPath = settings.saveDirectory;
            if (!System.IO.Directory.Exists(dirPath)) System.IO.Directory.CreateDirectory(dirPath);

            try
            {
                BakeAndSaveTiles(settings, dirPath);
                string lodPath = BakeAndSaveFullLOD(settings, dirPath, sceneName);

                if (!string.IsNullOrEmpty(lodPath))
                {
                    UpdateMapGridDataSO(settings, dirPath, sceneName, lodPath);
                }

                Debug.Log($"[MapBaker] 맵 베이킹 완료! (타일 {settings.Cols * settings.Rows}개 및 LOD 갱신 완료)");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void BakeAndSaveTiles(MapBakeSettingsSO settings, string dirPath)
        {
            int totalCount = settings.Cols * settings.Rows;

            for (int r = 0; r < settings.Rows; r++)
            {
                for (int c = 0; c < settings.Cols; c++)
                {
                    float progress = (float)(r * settings.Cols + c) / totalCount;
                    EditorUtility.DisplayProgressBar("맵 타일 굽는 중...", $"[{c},{r}] 타일 렌더링 중... ({(progress * 100):0.0}%)", progress);

                    Texture2D tileTex = MapBakeProcessor.CaptureTile(settings, c, r);
                    if (tileTex == null) continue;

                    string tilePath = $"{dirPath}/Tile_{c}_{r}.png";
                    System.IO.File.WriteAllBytes(tilePath, tileTex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tileTex);
                }
            }
        }

        private static string BakeAndSaveFullLOD(MapBakeSettingsSO settings, string dirPath, string sceneName)
        {
            EditorUtility.DisplayProgressBar("맵 타일 굽는 중...", "전체 맵 LOD 텍스처 생성 중...", 0.99f);

            Texture2D lodTex = MapBakeProcessor.CaptureFullMapLOD(settings, 2048);
            if (lodTex == null) return null;

            string lodPath = $"{dirPath}/{sceneName}_FullLOD.png";
            System.IO.File.WriteAllBytes(lodPath, lodTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(lodTex);

            AssetDatabase.Refresh();

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(lodPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            return lodPath;
        }

        private static void UpdateMapGridDataSO(MapBakeSettingsSO settings, string dirPath, string sceneName, string lodPath)
        {
            string soPath = $"{dirPath}/{sceneName}_MapGridData.asset";
            MapGridDataSO gridData = AssetDatabase.LoadAssetAtPath<MapGridDataSO>(soPath);

            if (gridData == null)
            {
                gridData = ScriptableObject.CreateInstance<MapGridDataSO>();
                AssetDatabase.CreateAsset(gridData, soPath);
            }

            gridData.sceneName = sceneName;
            gridData.saveDirectory = settings.saveDirectory;
            gridData.totalCols = settings.Cols;
            gridData.totalRows = settings.Rows;
            gridData.tileSize = settings.tileSize;

            Vector2 extents = new Vector2(settings.totalMapSize.x / 2f, settings.totalMapSize.y / 2f);
            gridData.worldMinBounds = new Vector2(settings.centerPosition.x - extents.x, settings.centerPosition.z - extents.y);
            gridData.worldMaxBounds = new Vector2(settings.centerPosition.x + extents.x, settings.centerPosition.z + extents.y);

            gridData.fullMapLOD = AssetDatabase.LoadAssetAtPath<Texture2D>(lodPath);

            EditorUtility.SetDirty(gridData);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region [5] 에셋 유틸리티 및 레이어 동기화 (Utilities)

        private static void SyncAllLayersFixed(MapBakeSettingsSO settings)
        {
            bool isChanged = false;

            while (settings.layerColors.Count < 32) { settings.layerColors.Add(new LayerColorPair()); isChanged = true; }
            while (settings.outlineSettings.Count < 32) { settings.outlineSettings.Add(new LayerOutlineSetting()); isChanged = true; }

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);

                LayerColorPair tempColorPair = settings.layerColors[i];
                bool colorPairChanged = false;

                if (tempColorPair.layerName != layerName)
                {
                    tempColorPair.layerName = layerName;
                    colorPairChanged = true;
                }
                if (tempColorPair.color == default(Color))
                {
                    tempColorPair.color = Color.white;
                    colorPairChanged = true;
                }

                if (colorPairChanged)
                {
                    settings.layerColors[i] = tempColorPair;
                    isChanged = true;
                }

                if (settings.outlineSettings[i].layerName != layerName)
                {
                    settings.outlineSettings[i].layerName = layerName;
                    if (settings.outlineSettings[i].outlineColor == default(Color))
                    {
                        settings.outlineSettings[i].outlineColor = Color.black;
                        settings.outlineSettings[i].outlineColor.a = 1f;
                        settings.outlineSettings[i].outlineThickness = 2;
                        settings.outlineSettings[i].depthThreshold = 0.001f;
                    }
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        #endregion
    }
}
#endif