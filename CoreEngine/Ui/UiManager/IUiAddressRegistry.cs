using CoreEngine.Helpers;
using CoreEngine.Hub;
using System;

namespace CoreEngine.UI
{
    public interface IUiAddressRegistry : IActor
    {
        string GetAddress<T>() where T : IUi;
        string GetAddress(Type uiType);
        string GetAddress(IAddressableUi addressableUi);
    }
}
