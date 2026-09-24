using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [System.Serializable]
    public class TestRecord : BaseRecord
    {
        private string name;
        private int level;
        private float damage;
        private bool enabled;

        public string Name => name;
        public int Level => level;
        public float Damage => damage;
        public bool Enabled => enabled;
    }
}
