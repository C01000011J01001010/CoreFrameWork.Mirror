using CoreEngine;

namespace CoreEngine.UI.Test
{
    public class TestPreloadedUi : BaseUi
    {
        protected override void OnShow()
        {
            UnityEngine.Debug.Log("[TestPreloadedUi] OnShow");
        }

        protected override void OnHide()
        {
            UnityEngine.Debug.Log("[TestPreloadedUi] OnHide");
        }
    }
}