using UnityEngine;

namespace CoreEngine.DesignPattern.Singleton.Test
{
    [SingletonSODirectory(Constants.DataDirectory + "/TestDirectory")]
    public class TestSingletonSO1 : SingletonSO<TestSingletonSO1>
    {

    }
}

