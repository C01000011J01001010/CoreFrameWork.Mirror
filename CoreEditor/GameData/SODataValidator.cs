using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;
using CoreEngine;

namespace CoreEditor.GameData
{
    public class SODataValidator : EditorWindow
    {
        private Vector2 _scrollPosition;

        private List<TypeStatus> _tableStatuses = new();
        private List<TypeStatus> _registryStatuses = new();

        // 하위 폴더명 강제 (상수화)
        private const string TableFolderName = "Table";
        private const string RegistryFolderName = "Registry";

        [MenuItem(Constants.ToolRootGameData + "SO Data Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SODataValidator>("SO Data Validator");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Game Data SO 대시보드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"{nameof(_DataTable)} 및 {nameof(_AssetRegistry)}를 상속받은 상세 클래스들의 SO 객체 상태를 확인하고 관리합니다.", MessageType.Info);
            EditorGUILayout.Space();

            DrawPathSettings();
            EditorGUILayout.Space();

            if (GUILayout.Button("🔄 전체 상태 새로고침", GUILayout.Height(30)))
            {
                Refresh();
            }

            if (GUILayout.Button("➡️ Preload 관리자 열기", GUILayout.Height(30)))
            {
                GetWindow<PreloadDashboard>("Preload Manager").Show();
            }

            EditorGUILayout.Space();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawSection("📊 Tables", _tableStatuses);
            EditorGUILayout.Space();
            DrawSection("📇 Registries", _registryStatuses);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            DrawBottomActions();
        }

        // =========================================================
        // [UI 그리기 로직]
        // =========================================================

        private void DrawPathSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📁 저장 경로 설정 (탐색기 선택)", EditorStyles.boldLabel);

            var settings = SODataValidatorSettings.Instance;

            EditorGUI.BeginChangeCheck();

            // 이제 경로 입력은 단 한 줄로 끝납니다. (ref로 public 필드 직접 참조)
            DrawFolderPickerRow("기본 저장 경로", ref settings.BaseSavePath);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            // 하위 구조 안내 (읽기 전용 표시)
            GUI.contentColor = Color.gray;
            EditorGUILayout.LabelField($" └─ Table 저장 경로: {settings.BaseSavePath}/{TableFolderName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($" └─ Registry 저장 경로: {settings.BaseSavePath}/{RegistryFolderName}", EditorStyles.miniLabel);
            GUI.contentColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawFolderPickerRow(string label, ref string pathValue)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));

            // 텍스트 직접 입력 방지
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(pathValue);
            EditorGUI.EndDisabledGroup();

            // 📂 탐색기 열기 버튼
            if (GUILayout.Button("📂", GUILayout.Width(30)))
            {
                string startPath = Application.dataPath;
                if (!string.IsNullOrEmpty(pathValue))
                {
                    string projectPath = Path.GetDirectoryName(Application.dataPath);
                    startPath = $"{projectPath}/{pathValue}";
                }

                string absolutePath = EditorUtility.OpenFolderPanel($"{label} 선택", startPath, "");

                if (!string.IsNullOrEmpty(absolutePath))
                {
                    if (absolutePath.StartsWith(Application.dataPath))
                    {
                        pathValue = "Assets" + absolutePath.Substring(Application.dataPath.Length);
                        GUI.FocusControl(null);
                    }
                    else
                    {
                        Debug.LogWarning("[SO Data Validator] 프로젝트 내부(Assets 하위)의 폴더만 선택할 수 있습니다.");
                    }
                }
            }

            // 해당 폴더 핑(Ping) 기능
            if (GUILayout.Button("확인", GUILayout.Width(50)))
            {
                PingFolder(pathValue);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(string title, List<TypeStatus> statuses)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (statuses.Count == 0)
            {
                EditorGUILayout.LabelField("  발견된 상세 클래스가 없습니다.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var status in statuses)
                {
                    DrawStatusRow(status);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(TypeStatus status)
        {
            EditorGUILayout.BeginHorizontal();

            string icon = status.InstanceCount == 1 ? "✅" : (status.InstanceCount == 0 ? "❌" : "⚠️");
            EditorGUILayout.LabelField($"{icon} {status.TargetType.Name}", GUILayout.Width(250));

            if (status.InstanceCount == 0)
            {
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("객체 없음 (생성 필요)");
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else if (status.InstanceCount == 1)
            {
                GUI.contentColor = Color.green;
                EditorGUILayout.LabelField("정상");
                GUI.contentColor = Color.white;

                if (GUILayout.Button("선택", GUILayout.Width(50)))
                {
                    PingAsset(status.AssetPaths[0]);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField($"중복! ({status.InstanceCount}개 존재)");
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // 트리 구조로 중복된 에셋 개별 확인
                EditorGUI.indentLevel++;
                for (int i = 0; i < status.AssetPaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.contentColor = Color.gray;
                    EditorGUILayout.LabelField($"└─ {status.AssetPaths[i]}", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;

                    if (GUILayout.Button("선택", GUILayout.Width(50)))
                    {
                        PingAsset(status.AssetPaths[i]);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawBottomActions()
        {
            var missingCount = _tableStatuses.Count(s => s.InstanceCount == 0) +
                               _registryStatuses.Count(s => s.InstanceCount == 0);

            GUI.enabled = missingCount > 0;
            if (GUILayout.Button($"🚀 누락된 에셋 일괄 생성하기 ({missingCount}개)", GUILayout.Height(40)))
            {
                GenerateMissingAssets();
            }
            GUI.enabled = true;
        }

        // =========================================================
        // [데이터 처리 로직]
        // =========================================================

        private void Refresh()
        {
            var tableTypes = TypeCache.GetTypesDerivedFrom<_DataTable>()
                .Where(t => !t.IsAbstract && !t.IsGenericType).ToList();

            var registryTypes = TypeCache.GetTypesDerivedFrom<_AssetRegistry>()
                .Where(t => !t.IsAbstract && !t.IsGenericType).ToList();

            _tableStatuses = AnalyzeTypes(tableTypes);
            _registryStatuses = AnalyzeTypes(registryTypes);
        }

        private List<TypeStatus> AnalyzeTypes(List<Type> types)
        {
            var result = new List<TypeStatus>();
            foreach (var type in types)
            {
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
                result.Add(new TypeStatus
                {
                    TargetType = type,
                    InstanceCount = guids.Length,
                    AssetPaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList()
                });
            }
            return result.OrderBy(s => s.TargetType.Name).ToList();
        }

        private void GenerateMissingAssets()
        {
            var settings = SODataValidatorSettings.Instance;

            // 상수 기반으로 자동 조합된 하위 폴더 경로
            string tablePath = $"{settings.BaseSavePath}/{TableFolderName}";
            string registryPath = $"{settings.BaseSavePath}/{RegistryFolderName}";

            EnsureDirectoryExists(tablePath);
            EnsureDirectoryExists(registryPath);

            GenerateForStatuses(_tableStatuses, tablePath);
            GenerateForStatuses(_registryStatuses, registryPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Refresh();

            Debug.Log($"[SO Data Validator] 에셋 생성 완료! (기본 경로: {settings.BaseSavePath})");
        }

        private void GenerateForStatuses(List<TypeStatus> statuses, string targetPath)
        {
            foreach (var status in statuses.Where(s => s.InstanceCount == 0))
            {
                var instance = ScriptableObject.CreateInstance(status.TargetType);
                string assetPath = $"{targetPath}/{status.TargetType.Name}.asset";
                AssetDatabase.CreateAsset(instance, assetPath);
            }
        }

        // =========================================================
        // [유틸리티]
        // =========================================================

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void PingFolder(string path)
        {
            EnsureDirectoryExists(path);
            AssetDatabase.Refresh();

            UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            if (folderObj != null)
            {
                Selection.activeObject = folderObj;
                EditorGUIUtility.PingObject(folderObj);
            }
            else
            {
                Debug.LogWarning($"[SO Data Validator] 경로를 찾을 수 없습니다: {path}");
            }
        }

        private void PingAsset(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private class TypeStatus
        {
            public Type TargetType;
            public int InstanceCount;
            public List<string> AssetPaths;
        }
    }
}