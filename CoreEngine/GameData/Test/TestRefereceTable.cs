using UnityEngine;

namespace CoreEngine.GameData.Test
{
    using TestSpriteId = AssetId<Sprite, TestSpriteRegistry>;
    using TestRecordId = RecordId<TestRecord, TestTable>;

    public sealed class TestReferenceRecord : BaseRecord
    {
        [TableColumn, SerializeField] private TestSpriteId spriteIdTest;
        [TableColumn, SerializeField] private TestSpriteId[] spriteIdArrayTest;

        [TableColumn, SerializeField] private TestRecordId recordIdTest;
        [TableColumn, SerializeField] private TestRecordId[] recordIdArrayTest;

        public Sprite TestSprite => spriteIdTest.Get();
        public TestRecord TestRecord => recordIdTest.Get();
    }
    public sealed class TestRefereceTable : BaseTable<TestReferenceRecord> { }
}
    
