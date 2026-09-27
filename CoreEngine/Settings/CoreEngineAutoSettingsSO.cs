using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Settings
{
    public interface IGameDataLabelSetter
    {
        void SetGameDataLabel(string newLabel);
    }

    public class CoreEngineAutoSettingsSO : BaseGameSettingsSO<CoreEngineAutoSettingsSO>, IGameDataLabelSetter
    {
        [SerializeField, HideInInspector]
        private string _gameDataLabel;
        public string GameDataLabel => _gameDataLabel;

        [SerializeField, HideInInspector]
        private List<string> _managedSceneGUIDs = new List<string>();
        public List<string> ManagedSceneGUIDs => _managedSceneGUIDs;

        void IGameDataLabelSetter.SetGameDataLabel(string newLabel)
        {
            _gameDataLabel = newLabel;
        }

#if UNITY_EDITOR
        // 에디터 트래커가 호출할 관리 메서드
        public void SyncManagedScene(string guid, bool exists)
        {
            if (exists && !_managedSceneGUIDs.Contains(guid))
                _managedSceneGUIDs.Add(guid);
            else if (!exists && _managedSceneGUIDs.Contains(guid))
                _managedSceneGUIDs.Remove(guid);

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

