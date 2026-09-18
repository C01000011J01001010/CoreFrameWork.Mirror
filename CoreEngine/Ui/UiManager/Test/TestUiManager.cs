using System;

namespace CoreEngine.UI.Test
{
    public enum TestUiType
    {
        Preloaded,
        Addressable,
    }

    public class TestUiManager : BaseUiManager<TestUiType>
    {
        protected override Type GetConcreteUiType(TestUiType uiEnum)
        {
            return uiEnum switch
            {
                TestUiType.Preloaded => typeof(TestPreloadedUi),
                TestUiType.Addressable => typeof(TestAddressableUi),
                _ => null
            };
        }
    }
}