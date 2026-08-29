

namespace CoreEngine.Manager.Pool.Test
{
    public enum TestPoolType
    {
        Poolable,
        NotPoolable
    }

    public class TestObjectPoolManager : ObjectPoolManager<TestPoolType>
    {

    }
}
