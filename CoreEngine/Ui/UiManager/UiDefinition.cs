using System;
using UnityEngine;

namespace CoreEngine.UI
{
    public class UiDefinition<TUiType> where TUiType : Enum
    {
        public readonly TUiType UiType;
        public readonly IUi UiObject;
        public readonly string UiAddress;

        public UiDefinition(TUiType uiType, IUi uiObject, string uiAdrress = null)
        {
            UiType = uiType;
            UiObject = uiObject;
            UiAddress = uiAdrress;
        }
    }
            
}

