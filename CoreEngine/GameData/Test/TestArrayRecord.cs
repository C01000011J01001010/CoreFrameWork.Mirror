using System;
using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [Serializable]
    public class TestArrayRecord : BaseDataRecord
    {
        [TableColumn]
        float[] floatArray;
    }
}

