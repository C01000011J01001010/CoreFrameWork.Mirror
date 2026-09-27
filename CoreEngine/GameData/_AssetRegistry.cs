using UnityEngine;

namespace CoreEngine.GameData
{
    internal abstract class _AssetRegistry : ScriptableObject
    {
        public abstract void InitializeRuntimeCache();
    }
}
