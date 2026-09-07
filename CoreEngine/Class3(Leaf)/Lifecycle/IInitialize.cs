
using System.Collections;

namespace CoreEngine
{
    public interface IInitialize
    {
        IEnumerator Initialize();

        void Exit();
    }
}
