using CoreEngine.Helpers;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreEngine.DesignPattern.Singleton
{
    public abstract class SingletonSO<T> : ScriptableObject
        where T : SingletonSO<T>
    {
        protected static string DefaultDirectory
        {
            get
            {
                var attribute = typeof(T)
                    .GetCustomAttributes(typeof(SingletonSO_DirectoryAttribute), true);

                if (attribute.Length > 0)
                    return ((SingletonSO_DirectoryAttribute)attribute[0]).Directory;

                return Constants.ProjectDirectory;
            }
        }

        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
#if UNITY_EDITOR
                // 에셋 데이터베이스에서 기존 설정 에셋 검색
                string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = AssetDatabase.LoadAssetAtPath<T>(path);
                }
                else
                {
                    // 하나도 없다면 정해진 경로에 자동 생성
                    _instance = CreateInstance<T>();
                    //string folderPath = //"Assets/Settings/CoreFramework";

                    if (!Directory.Exists(DefaultDirectory))
                    {
                        Directory.CreateDirectory(DefaultDirectory);
                    }

                    string assetPath = $"{DefaultDirectory}/{typeof(T).Name}.asset";
                    AssetDatabase.CreateAsset(_instance, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    LogHelper.Log($"{typeof(T).Name}.asset이 자동으로 생성되었습니다: {assetPath}"
                        ,LogColor.Cyan);
                }
#endif
                return _instance;
            }
        }

#if UNITY_EDITOR
        // SO에셋 중복 생성 차단
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                string path = AssetDatabase.GetAssetPath(this); // 현재 생성되려는 중복 객체의 경로
                string originalPath = AssetDatabase.GetAssetPath(_instance); // 원본의 경로

                LogHelper.LogError($"{typeof(T).Name} 객체가 이미 존재합니다! \n원본: {originalPath} \n삭제됨: {path}");

                // 메모리에서 즉시 파괴 (Destroy 대신 즉각적인 처리)
                DestroyImmediate(this, true);
                return;
            }
        }
#endif
        // 도메인 리로드 및 런타임에 AutoPreloadAsset로 자동 로드시 싱글톤 처리
        protected virtual void OnEnable()
        {
            _instance = this as T;
        }
    }
}

