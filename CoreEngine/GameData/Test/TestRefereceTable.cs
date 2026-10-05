using UnityEngine;

namespace CoreEngine.GameData.Test
{
    using TestSpriteId = AssetId<Sprite, TestSpriteRegistry>;
    using TestForeignKey = ForeignKey<TestRecord, TestTable>;

    [System.Serializable]
    public sealed class TestReferenceRecord : BaseRecord
    {
        [TableColumn, SerializeField] private string tempPK;
        [TableColumn, SerializeField] private TestSpriteId spriteIdTest;
        [TableColumn, SerializeField] private TestForeignKey recordIdTest;

        [TableColumn, SerializeField] private TestSpriteId[] spriteIdArrayTest;
        [TableColumn, SerializeField] private TestForeignKey[] recordIdArrayTest;

        public Sprite TestSprite => spriteIdTest.Get();
        public TestRecord TestRecord => recordIdTest.Get();

        protected override string GetPrimaryKey()
        {
            return tempPK.ToString();
        }
    }
    public sealed class TestRefereceTable : BaseTable<TestReferenceRecord> { }
}
    
