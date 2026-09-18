using CoreEngine.Helpers;
using System;
using System.Collections.Generic;

namespace CoreEngine.UI
{
    public abstract class BaseUiAddressRegistry : BaseActor, IUiAddressRegistry
    {
        protected readonly Dictionary<Type, string/*Address*/> UiAddressMap;

        protected void Add<T>(string Address) where T : IUi
            => Add(typeof(T), Address);
        protected void Add(Type uiType, string Address)
        {
            UiAddressMap[uiType] = Address;
        }

        public string GetAddress<T>() where T : IUi
            => GetAddress(typeof(T));

        public string GetAddress(IAddressableUi addressableUi)
            => GetAddress(addressableUi.GetType());

        public string GetAddress(Type uiType)
        {
            if (UiAddressMap.TryGetValue(uiType, out var address) && !string.IsNullOrEmpty(address))
            {
                return address;
            }
            else
            {
                LogHelper.LogWarning($"{uiType.Name}의 Address가 정의되지 않음");
                return null;
            }
        }

        
    }
}

