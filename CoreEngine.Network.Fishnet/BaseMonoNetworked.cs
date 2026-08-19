using FishNet.Object;
using UnityEngine;

namespace CoreEngine.Network
{
    public class BaseMonoNetworked : NetworkBehaviour
    {
        private void Awake()
        {
            if (GetComponents(GetType()).Length > 1)
            {
                Destroy(this);
            }
        }
    }
}