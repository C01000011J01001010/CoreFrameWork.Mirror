#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreEditor
{
    /// <summary>
    /// 에디터 전용 범용 유틸리티 (ScriptableObject 생성, UI 드로잉 등)
    /// </summary>
    public static class UtilitySettingData
    {
        /// <summary>
        /// [제네릭] ScriptableObject 프로필 필드를 그리고, 없을 경우 생성 버튼을 띄워주는 통합 UI 헬퍼
        /// </summary>
        /// <typeparam name="T">생성할 ScriptableObject의 타입</typeparam>
        /// <param name="profileProp">바인딩할 SerializedProperty</param>
        /// <param name="missingHelpText">프로필이 없을 때 띄울 경고 문구</param>
        /// <param name="defaultFileName">저장 창에 뜰 기본 파일명 접두사</param>
        /// <param name="onCreated">생성 직후 객체와 저장경로를 넘겨주는 콜백 (추가 세팅용)</param>
        public static void DrawProfileSetupGUI<T>(
            SerializedProperty profileProp,
            string missingHelpText,
            string defaultFileName,
            Action<T, string> onCreated = null) where T : ScriptableObject
        {
            // 1. 프로필 연결 필드 렌더링
            EditorGUILayout.PropertyField(profileProp);

            // 2. 프로필이 할당되지 않았다면 생성 유도 UI 표시
            if (profileProp.objectReferenceValue == null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(missingHelpText, MessageType.Warning);

                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button($"✨ 새로운 {typeof(T).Name} 생성 (Create Profile)", GUILayout.Height(35)))
                {
                    // 에셋 생성 시도
                    T newAsset = CreateScriptableObjectAsset<T>(defaultFileName, onCreated);

                    if (newAsset != null)
                    {
                        // 생성된 에셋을 Property에 즉시 바인딩
                        profileProp.objectReferenceValue = newAsset;
                        profileProp.serializedObject.ApplyModifiedProperties();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }

        /// <summary>
        /// [제네릭] 실제 ScriptableObject 파일을 디스크에 생성하는 코어 엔진
        /// </summary>
        private static T CreateScriptableObjectAsset<T>(string defaultFileNamePrefix, Action<T, string> onCreated) where T : ScriptableObject
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "UntitledScene";

            string savePath = EditorUtility.SaveFilePanelInProject(
                $"새 {typeof(T).Name} 저장",
                $"{defaultFileNamePrefix}_{sceneName}",
                "asset",
                "저장할 위치와 파일명을 지정하세요."
            );

            if (string.IsNullOrEmpty(savePath)) return null;

            // 인스턴스 생성
            T newAsset = ScriptableObject.CreateInstance<T>();

            // 🌟 해당 타입만의 고유한 세팅(예: 저장 폴더 경로 기록 등)을 외부(람다)에서 주입받아 실행
            onCreated?.Invoke(newAsset, savePath);

            // 디스크에 저장
            AssetDatabase.CreateAsset(newAsset, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(newAsset);
            Debug.Log($"[CoreEditorUtility] 새 프로필 에셋이 생성되었습니다: {savePath}");

            return newAsset;
        }
    }
}
#endif