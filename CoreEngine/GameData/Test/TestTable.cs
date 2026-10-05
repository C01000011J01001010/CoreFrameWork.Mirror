using CoreEngine.Helpers;
using System.Text;
using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [System.Serializable]
    public class TestRecord : BaseRecord
    {
        [TableColumn, SerializeField] private int intTest;
        [TableColumn, SerializeField] private float floatTest;
        [TableColumn, SerializeField] private bool boolTest;
        [TableColumn, SerializeField] private string stringTest;

        [TableColumn, SerializeField] private int[] intArrayTest;
        [TableColumn, SerializeField] private float[] floatArrayTest;
        [TableColumn, SerializeField] private bool[] boolArrayTest;
        [TableColumn, SerializeField] private string[] stringArrayTest;

        protected override ulong BakeID()
        {
#if UNITY_EDITOR
            return GetMashedKey(intTest, floatTest, boolTest, stringTest);
#endif
        }
    }

    public class TestTable : BaseTable<TestRecord> { }
}

