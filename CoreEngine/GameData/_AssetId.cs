using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    [Serializable]
    public abstract class _AssetId
    {
        [SerializeField]
        protected int id;

        public int Id => id;

        // Unity 직렬화를 위한 기본 생성자
        protected _AssetId() { }

        // 코드에서 new로 생성할 때 쓸 생성자
        protected _AssetId(int id) { this.id = id; }

        // 공통 로드/해제 규약
        public abstract Task LoadAsync();
        public abstract void Release();
    }
}