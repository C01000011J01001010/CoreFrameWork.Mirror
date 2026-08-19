using UnityEngine;

namespace CoreEngine
{
    public class Constants
    {
        private static LayerMask GetLayer(params int[] index)
        {
            LayerMask result = 0;
            foreach (int i in index)
            {
                result |= 1 << i;
            }
            return result;
        }
    }
}

