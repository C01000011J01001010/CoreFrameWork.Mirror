using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [System.Serializable]
    public sealed class TestRecord : BaseRecord
    {
        [TableColumn, SerializeField] private int intTest;
        [TableColumn, SerializeField] private float floatTest;
        [TableColumn, SerializeField] private bool boolTest;
        [TableColumn, SerializeField] private string stringTest;

        [TableColumn, SerializeField] private int[] intArrayTest;
        [TableColumn, SerializeField] private float[] floatArrayTest;
        [TableColumn, SerializeField] private bool[] boolArrayTest;
        [TableColumn, SerializeField] private string[] stringArrayTest;


        protected override string GetPrimaryKey()
        {
            return GetCompositeKey(intTest, floatTest, boolTest, stringTest);
        }

    }

    public sealed class TestTable : BaseTable<TestRecord> { }
}

