using CoreEngine.Helpers;
using CoreEngine.Hub;
using System;

namespace CoreEngine.UI
{
    public interface IUiAddressRegistry : IActor
    {
        string GetAddress<T>() where T : IAddressableUi;
        string GetAddress(IAddressableUi addressableUi);
        string GetAddress(Type uiType);
    }
}
