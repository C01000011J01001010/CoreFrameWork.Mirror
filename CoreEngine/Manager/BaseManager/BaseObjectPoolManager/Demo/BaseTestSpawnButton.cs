using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.Pool.Test
{
    [RequireComponent(typeof(Button))]
    public abstract class BaseTestSpawnButton<TPoolType> : MonoBehaviour
        where TPoolType : System.Enum
    {

        public TPoolType targetPoolType;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClickSpawn);
        }

        protected abstract void OnClickSpawn();
    }
}
