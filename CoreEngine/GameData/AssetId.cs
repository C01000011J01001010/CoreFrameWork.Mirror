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
        private int id;
        public int Id => id;

        public AssetId(int id)
        {
            this.id = id;
        }

        public TAsset Get()
        {
            if (id <= 0) return null;
            return AssetRegistryRouter.Get(RegistryType, id) as TAsset;
        }

        public static implicit operator AssetId<TAsset, TRegistry>(int id)
            => new AssetId<TAsset, TRegistry>(id);

        public bool Equals(AssetId<TAsset, TRegistry> other) 
            => id == other.id;

        public override bool Equals(object obj) 
            => obj is AssetId<TAsset, TRegistry> other && Equals(other);

        public override int GetHashCode() => id.GetHashCode();

        public static bool operator ==(AssetId<TAsset, TRegistry> left, AssetId<TAsset, TRegistry> right) 
            => left.Equals(right);

        public static bool operator !=(AssetId<TAsset, TRegistry> left, AssetId<TAsset, TRegistry> right) 
            => !left.Equals(right);
    }
}