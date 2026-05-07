using System.Collections.Generic;
using MmoGame.World;
using UnityEditor;
using UnityEngine;

namespace MmoGame.Editor
{
    /// <summary>
    /// Idempotent setup for the map prefab registry. Reads a hard-coded
    /// list of (logical name → asset path) bindings, loads each prefab, and
    /// writes/refreshes Assets/Resources/MapPrefabRegistry.asset. Re-run any
    /// time after importing a new Synty pack to add fresh entries.
    /// </summary>
    public static class MapSetup
    {
        const string RegistryPath = "Assets/Resources/MapPrefabRegistry.asset";
        const string ResourcesFolder = "Assets/Resources";

        // Logical name → asset path. Stable names live in map JSON, so paths can
        // shift without rewriting maps.
        static readonly (string name, string path)[] DefaultBindings =
        {
            ("ground_grass_tile",  "Assets/Synty/PolygonKnights/Prefabs/Environments/SM_Env_Tile_Grass_01.prefab"),
            ("ground_mound",       "Assets/Synty/PolygonKnights/Prefabs/Environments/SM_Env_GroundMound_01.prefab"),
            ("flower",             "Assets/Synty/PolygonKnights/Prefabs/Environments/SM_Env_Flower_01.prefab"),
            ("path_cobble",        "Assets/Synty/PolygonKnights/Prefabs/Environments/SM_Env_Path_Cobble_01.prefab"),
            ("tent_blue",          "Assets/Synty/PolygonKnights/Prefabs/Buildings/SM_Bld_Tent_01.prefab"),
            ("tent_red",           "Assets/Synty/PolygonKnights/Prefabs/Buildings/SM_Bld_Tent_02.prefab"),
            ("rockwall_straight",  "Assets/Synty/PolygonKnights/Prefabs/Buildings/SM_Bld_Rockwall_Straight_01.prefab"),
            ("rockwall_archway",   "Assets/Synty/PolygonKnights/Prefabs/Buildings/SM_Bld_Rockwall_Archway_01.prefab"),
            ("campfire",           "Assets/Synty/PolygonKnights/Prefabs/Props/SM_Prop_CampFire_01.prefab"),
            ("brazier",            "Assets/Synty/PolygonKnights/Prefabs/Props/SM_Prop_Brazier_01.prefab"),
            ("banner_1",           "Assets/Synty/PolygonKnights/Prefabs/Props/SM_Prop_Banner_01.prefab"),
            ("banner_2",           "Assets/Synty/PolygonKnights/Prefabs/Props/SM_Prop_Banner_02.prefab"),
        };

        [MenuItem("MmoGame/Setup Map Registry")]
        public static void Run()
        {
            EnsureResourcesFolder();

            var registry = AssetDatabase.LoadAssetAtPath<MapPrefabRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<MapPrefabRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            var entries = new List<MapPrefabRegistry.Entry>();
            int loaded = 0, missing = 0;

            foreach (var (name, path) in DefaultBindings)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[MapSetup] {path} not found — skipping '{name}'.");
                    missing++;
                    continue;
                }
                entries.Add(new MapPrefabRegistry.Entry { name = name, prefab = prefab });
                loaded++;
            }

            registry.entries = entries.ToArray();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MapSetup] Registry has {loaded} entries (skipped {missing}). Saved {RegistryPath}.");
        }

        static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}
