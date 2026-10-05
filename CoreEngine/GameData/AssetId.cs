using CoreEngine.Helpers;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    // [최적화] 완벽한 struct 값 타입. 상속(class) 제거.
    [Serializable]
    public struct AssetId<TAsset, TRegistry> : IIdentifiable, IEquatable<AssetId<TAsset, TRegistry>>
        where TAsset : Object
        where TRegistry : BaseAssetRegistry<TAsset>
    {
        public readonly static Type RegistryType = typeof(TRegistry);

        [SerializeField]
        private ulong _id;
        public ulong ID => _id;

        public AssetId(ulong id)
        {
            _id = id;
        }

        public AssetId(string numericString)
        {
            ulong.TryParse(numericString, out this._id);
        }

        public TAsset Get()
        {
            if (_id <= 0) return null;
            return AssetRegistryRouter.Get(RegistryType, ID) as TAsset;
        }
        

        public bool Equals(AssetId<TAsset, TRegistry> other) 
            => _id == other._id;

        public override bool Equals(object obj) 
            => obj is AssetId<TAsset, TRegistry> other && Equals(other);

        public override int GetHashCode() => _id.GetHashCode();

        public static bool operator ==(AssetId<TAsset, TRegistry> left, AssetId<TAsset, TRegistry> right) 
            => left.Equals(right);

        public static bool operator !=(AssetId<TAsset, TRegistry> left, AssetId<TAsset, TRegistry> right) 
            => !left.Equals(right);
    }
}