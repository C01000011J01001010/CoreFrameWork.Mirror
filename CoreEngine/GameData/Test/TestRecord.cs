using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [System.Serializable]
    public class TestRecord : BaseDataRecord
    {
        [TableColumn, SerializeField] private string name;
        [TableColumn, SerializeField] private int level;
        [TableColumn, SerializeField] private float damage;
        [TableColumn, SerializeField] private bool enabled;
        [TableColumn, SerializeField] private AssetId<Sprite> spriteAsset;

        public string Name => name;
        public int Level => level;
        public float Damage => damage;
        public bool Enabled => enabled;
        public Sprite SpriteAsset => spriteAsset.Get();
    }
}
