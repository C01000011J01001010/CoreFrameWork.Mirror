using CoreEngine.DesignPattern.Singleton;

namespace CoreEditor
{
    [SingletonSODirectory(Constants.ToolSettingsDirectory)]
    public class BaseToolSettings<T> : SingletonSO<T> where T : BaseToolSettings<T>
    {
    }
}

