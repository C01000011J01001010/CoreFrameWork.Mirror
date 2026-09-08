using CoreEngine.DesignPattern.Singleton;

namespace CoreEngine.Director
{
    public class BaseDirector<Director> : Singleton<Director>, IDirector
        where Director : BaseDirector<Director>
    {
    }
}
