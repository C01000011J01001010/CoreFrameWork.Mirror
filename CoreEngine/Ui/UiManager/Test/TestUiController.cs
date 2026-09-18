using CoreEngine.Facades;
using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.UI.Test
{
    public class TestUiController : MonoBehaviour
    {
        [SerializeField] TestUiType _targetUiType;
        [SerializeField] private Button _showButton;
        [SerializeField] private Button _hideButton;

        TestUiManager _uiManager;

        private void OnDestroy()
        {
            _showButton?.onClick.RemoveAllListeners();
            _hideButton?.onClick.RemoveAllListeners();
        }

        private void Awake()
        {
            _showButton?.onClick.AddListener(OnShowButtonClick);
            _hideButton?.onClick.AddListener(OnHideButtonClick);
            _uiManager = CoreFacade.GetManager<TestUiManager>();
        }

        private void OnShowButtonClick()
        {
            _ = _uiManager.Show(_targetUiType);
        }

        private void OnHideButtonClick()
        {
            _uiManager.Hide(_targetUiType);
        }
    }
}

