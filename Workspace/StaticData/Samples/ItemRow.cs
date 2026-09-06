using System;
using CoreEngine.StaticData;
namespace CoreEngine.StaticData.Samples
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
