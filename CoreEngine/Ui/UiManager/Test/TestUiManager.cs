using System;
using UnityEngine;

namespace CoreEngine.UI.Test
{
    public enum TestUiType
    {

    }
    public class TestUiManager : BaseUiManager<TestUiType>
    {
        protected override Type GetConcreteUiType(TestUiType uiType)
        {
            throw new NotImplementedException();
        }
    }
}

