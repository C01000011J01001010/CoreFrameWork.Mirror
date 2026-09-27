using System;
using System.Threading.Tasks;

namespace CoreEngine.GameData
{
    [Serializable]
    public abstract class _AssetId
    {
        public abstract int Id { get; }

        // 공통 로드/해제 규약
        public abstract Task LoadAsync();
        public abstract void Release();
    }
}