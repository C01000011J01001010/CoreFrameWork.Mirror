using System;
using CoreEngine.GameData;
namespace CoreEngine.GameData.Samples
{
    [Serializable]
    public sealed class ItemRow : IStaticDataRow
    {
        public int id;
        public string displayName;
        public int price;
        public int Id => id;
    }
}
