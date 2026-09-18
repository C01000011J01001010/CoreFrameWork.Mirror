using UnityEngine;

namespace CoreEngine.UI.Test
{
    public class TestAddressableUi : BaseUi, IAddressableUi
    {
        [SerializeField] 
        private float _releaseDelay = 3;
        public float ReleaseDelay => _releaseDelay;

        protected override void OnShow()
        {
            Debug.Log("[TestAddressableUi] OnShow");
        }

        protected override void OnHide()
        {
            Debug.Log("[TestAddressableUi] OnHide");
        }
    }
}