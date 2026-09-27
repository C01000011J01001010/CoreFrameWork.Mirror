using UnityEngine;

namespace CoreEngine.GameData
{
    public abstract class _AssetRegistry : ScriptableObject
    {
        public abstract void InitializeRuntimeCache();
    }
}
