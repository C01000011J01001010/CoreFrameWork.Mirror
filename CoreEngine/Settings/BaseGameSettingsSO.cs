using CoreEngine.DesignPattern.Singleton;

namespace CoreEngine.Settings
{
    [AutoPreloadAsset]
    [SingletonSO_Directory(Constants.DataDirectory + "/Settings")]
    public class BaseGameSettingsSO<T> : SingletonSO<T> where T : BaseGameSettingsSO<T>
    {
    }
}

