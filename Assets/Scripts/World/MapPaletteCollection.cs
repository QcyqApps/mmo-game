using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmoGame.World
{
    /// <summary>
    /// User-curated palette for the Map Editor. Holds named categories
    /// (e.g. "Trees", "Roads", "Houses (Knights)") populated by drag &
    /// drop from the Project window. Replaces the firehose view of every
    /// scanned Synty prefab — most maps use a working set of 30–50 pieces,
    /// not the full catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "MapPalette", menuName = "MmoGame/Map Palette")]
    public class MapPaletteCollection : ScriptableObject
    {
        [Serializable]
        public class Category
        {
            public string name = "Untitled";
            public List<GameObject> prefabs = new();
            public bool expanded = true;
        }

        public List<Category> categories = new();
    }
}
