using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [System.Serializable]
    public class TestRecord : BaseDataRecord
    {
        [TableColumn] private string name;
        [TableColumn] private int level;
        [TableColumn] private float damage;
        [TableColumn] private bool enabled;
        [TableColumn] private AssetId<Sprite> spriteAsset;
        //[TableColumn] private int temp;

        public string Name => name;
        public int Level => level;
        public float Damage => damage;
        public bool Enabled => enabled;
        public Sprite SpriteAsset => spriteAsset.Get();
    }
}
