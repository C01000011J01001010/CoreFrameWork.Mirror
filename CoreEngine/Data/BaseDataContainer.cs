using System;
using UnityEngine;

namespace CoreEngine.Data
{
    public abstract class BaseDataContainer<Data> : BaseObjectContainer<Data>
        where Data : BaseData
    {
        public int GetIndex() => connectData.Index;
        public string GetNameTag() => connectData.NameTag;

        public BaseDataContainer() : base() { }
        public BaseDataContainer(Data InitialObject) : base(InitialObject) { }
    }
}
