using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    public abstract class _AssetRegistry : ScriptableObject
    {
        public abstract void InitializeRuntimeCache();

        public abstract Object GetAsset(ulong id);

        public abstract Task<Object> LoadAssetAsync(ulong id);

        public abstract void ReleaseAsset(ulong id);

    }
}
