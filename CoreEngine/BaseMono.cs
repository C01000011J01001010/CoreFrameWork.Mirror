using UnityEngine;
namespace CoreEngine
{
    public class BaseMono : MonoBehaviour
    {
        private void Awake()
        {
            if(GetComponents(GetType()).Length > 1)
            {
                Destroy(this);
            }
        }
    }
}
