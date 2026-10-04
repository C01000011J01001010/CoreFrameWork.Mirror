using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    

    [Serializable]
    public class AssetId<TAsset, TAssetRegistry> : _AssetId, IEquatable<AssetId<TAsset, TAssetRegistry>> 
        where TAsset : Object
        where TAssetRegistry : BaseAssetRegistry<TAsset>
    {
        public readonly static Type RegistryType = typeof(TAssetRegistry);
        // 부모 생성자 호출
        public AssetId() : base() { }
        public AssetId(int id) : base(id) { }

        public TAsset Get()
        {
            if (id <= 0) return null;
            return AssetRouter.Get(RegistryType, id) as TAsset;
        }

        public override Task LoadAsync()
        {
            if (id <= 0) return Task.CompletedTask;
            return AssetRouter.LoadAsync(RegistryType, id);
        }

        public override void Release()
        {
            if (id <= 0) return;
            AssetRouter.Release(RegistryType, id); // 라우터에 해제 요청
        }

        public static implicit operator AssetId<TAsset, TAssetRegistry>(int id) 
            => new AssetId<TAsset, TAssetRegistry>(id);

        public bool Equals(AssetId<TAsset, TAssetRegistry> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj) => Equals(obj as AssetId<TAsset, TAssetRegistry>);
        public override int GetHashCode() => id.GetHashCode();
        public static bool operator ==(AssetId<TAsset, TAssetRegistry> left, AssetId<TAsset, TAssetRegistry> right) => Equals(left, right);
        public static bool operator !=(AssetId<TAsset, TAssetRegistry> left, AssetId<TAsset, TAssetRegistry> right) => !Equals(left, right);
    }
}