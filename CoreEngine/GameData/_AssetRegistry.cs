using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.GameData
{
    public abstract class _AssetRegistry : ScriptableObject
    {
        public abstract void InitializeRuntimeCache();

        public abstract Object GetAsset(int id);

        public abstract Task<Object> LoadAssetAsync(int id);

        public abstract void ReleaseAsset(int id);

    }
}
