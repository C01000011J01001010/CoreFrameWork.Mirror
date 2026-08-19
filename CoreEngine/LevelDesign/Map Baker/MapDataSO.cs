using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.LevelDesign
{
    [CreateAssetMenu(fileName = "NewMapData", menuName = MenuNamesSO.DefaultMenu + "/LevelDesign/Map Data")]
    public class MapDataSO : ScriptableObject
    {
        [Header("Map Visual")]
        public Texture2D MapTexture;

        [Header("World Mapping Data")]
        public Vector2 WorldOrigin;
        public Vector2 WorldSize;
    }
}