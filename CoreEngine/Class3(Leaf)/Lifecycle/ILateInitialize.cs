using System.Collections;

namespace CoreEngine
{
    public interface ILateInitialize
    {
        IEnumerator LateInitialize();
    }
}
