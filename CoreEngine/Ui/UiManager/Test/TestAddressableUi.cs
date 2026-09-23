using UnityEngine;

namespace CoreEngine.UI.Test
{
    public class TestAddressableUi : BaseUi, IAddressableUi
    {
        [Header("Addressable 정책 결정"), SerializeField] 
        private UiReleasePolicy _releasePolicy = UiReleasePolicy.AfterSecond05;
        public float ReleaseDelay => (float)_releasePolicy;

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