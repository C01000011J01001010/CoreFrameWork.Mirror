using System;
using UnityEngine;
namespace CoreEngine.StaticData
{
    public class AssetId<TRegistry, TAsset>
        where TRegistry : AssetRegistry<TRegistry, TAsset>
        where TAsset : UnityEngine.Object
    {
        [SerializeField] private int _id;
        public int Id => _id;

        public TAsset Get()
        {
            if (AssetRegistry<TRegistry, TAsset>.Inst == null)
                throw new InvalidOperationException($"{typeof(TRegistry).Name} 인스턴스가 존재하지 않습니다.");
            return AssetRegistry<TRegistry, TAsset>.Get(_id);
        }
    }
}

