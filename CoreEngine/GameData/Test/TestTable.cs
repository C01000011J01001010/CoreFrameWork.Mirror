using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [CreateAssetMenu(fileName = nameof(TestTable), menuName = "CoreEngine/" + nameof(TestTable))]
    public class TestTable : BaseTable<TestRecord>
    {

    }
}

