using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    

    [Serializable]
    public class AssetId<TAsset> : _AssetId, IEquatable<AssetId<TAsset>> where TAsset : Object
    {
        [SerializeField] private int id;
        public override int Id => id;

        public AssetId(int id) { this.id = id; }

        public TAsset Get()
        {
            if (id <= 0) return null;
            return AssetRouter<TAsset>.Get(id);
        }

        public override Task LoadAsync()
        {
            if (id <= 0) return Task.CompletedTask;
            return AssetRouter<TAsset>.LoadAsync(id);
        }

        public override void Release()
        {
            if (id <= 0) return;
            AssetRouter<TAsset>.Release(id); // 라우터에 해제 요청
        }

        public static implicit operator AssetId<TAsset>(int id) => new AssetId<TAsset>(id);

        public bool Equals(AssetId<TAsset> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj) => Equals(obj as AssetId<TAsset>);
        public override int GetHashCode() => id.GetHashCode();
        public static bool operator ==(AssetId<TAsset> left, AssetId<TAsset> right) => Equals(left, right);
        public static bool operator !=(AssetId<TAsset> left, AssetId<TAsset> right) => !Equals(left, right);
    }
}