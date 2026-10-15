using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.GameData.Test
{
    using TestForeignKey = ForeignKey<TestRecord, TestTable>;
    using TestSpriteId = AssetId<Sprite, TestSpriteRegistry>;

    [System.Serializable]
    public sealed class TestReferenceRecord : BaseRecord
    {
        [TableColumn, SerializeField] private string tempPK;
        [TableColumn, SerializeField] private int tableId;

        [TableColumn, SerializeField] private TestSpriteId spriteIdTest;
        [TableColumn, SerializeField] private TestSpriteId[] spriteIdArrayTest;

        [TableColumn, SerializeField] private TestForeignKey testFK;

        public Sprite TestSprite => spriteIdTest.Get();
        public TestForeignKey TestFK => testFK;
    }
    public sealed class TestRefereceTable : BaseTable<TestReferenceRecord> 
    {
        public const uint TEST_INDEX = 1;
        protected override List<KeyValuePair<uint, GetForeignKeyHash>> GetReverseIndexBuilder()
        {
            return new()
            {
                new(TEST_INDEX, record => record.TestFK.ID)
            };
        }
    }
}
    
