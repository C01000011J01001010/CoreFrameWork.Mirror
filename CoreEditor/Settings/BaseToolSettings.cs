using CoreEngine.DesignPattern.Singleton;

namespace CoreEditor
{
    [SingletonSO_Directory(Constants.EditorDirectory)]
    public class BaseToolSettings<T> : SingletonSO<T> where T : BaseToolSettings<T>
    {
    }
}

