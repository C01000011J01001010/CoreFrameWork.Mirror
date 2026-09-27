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

        void IGameDataLabelSetter.SetGameDataLabel(string newLabel)
        {
            _gameDataLabel = newLabel;
        }
    }
}

