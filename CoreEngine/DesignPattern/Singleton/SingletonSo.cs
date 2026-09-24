using CoreEngine.Helpers;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreEngine.DesignPattern.Singleton
{
    public abstract class SingletonSO<T> : ScriptableObject
        where T : SingletonSO<T>
    {
        protected const string Prefix = "Assets/CoreFramework SO";
        protected static string DefaultDirectory
        {
            get
            {
                var attribute = typeof(T)
                    .GetCustomAttributes(typeof(SingletonSODirectory), true);

                if (attribute.Length > 0)
                    return ((SingletonSODirectory)attribute[0]).Derectory;

                return Prefix;
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
        private void Awake()
        {
            if (_instance == null) _instance = this as T;
            else
            {
                string path = AssetDatabase.GetAssetPath(_instance);
                LogHelper.LogError($"{typeof(T).Name} 객체가 path({path})에 존재합니다.");
                Destroy(this);
            }
        }
#endif
    }
}

