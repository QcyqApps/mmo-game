using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmoGame.World
{
    /// <summary>
    /// Lookup table mapping logical piece names (used in map JSON manifests)
    /// to actual prefab references. Populated once via the editor — JSON
    /// authors deal in stable names, asset paths can change without breaking
    /// every map.
    /// </summary>
    [CreateAssetMenu(fileName = "MapPrefabRegistry", menuName = "MmoGame/Map Prefab Registry")]
    public class MapPrefabRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string name;
            public GameObject prefab;
        }

        public Entry[] entries;

        Dictionary<string, GameObject> _cache;

        public GameObject Get(string name)
        {
            if (_cache == null)
            {
                _cache = new Dictionary<string, GameObject>(entries.Length);
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.name) && e.prefab != null)
                        _cache[e.name] = e.prefab;
            }
            return _cache.TryGetValue(name, out var p) ? p : null;
        }
    }
}
