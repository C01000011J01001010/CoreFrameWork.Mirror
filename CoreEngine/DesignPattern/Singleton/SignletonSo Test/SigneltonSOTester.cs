using CoreEngine.DesignPattern.Singleton.Test;
using UnityEngine;

public class SigneltonSOTester : MonoBehaviour
{
    private void Start()
    {
        if (TestSingletonSO1.Instance == null) ;
        if (TestSingletonSO2.Instance == null) ;
        //Debug.Log(TestSingletonSO1.Instance);
        //Debug.Log(TestSingletonSO2.Instance);
    }
}
