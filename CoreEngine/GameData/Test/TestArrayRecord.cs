using System;
using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [Serializable]
    public class TestArrayRecord : BaseRecord
    {
        [TableColumn]
        float[] floatArray;
    }
}

