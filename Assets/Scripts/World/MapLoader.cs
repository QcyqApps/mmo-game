using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MmoGame.World
{
    /// <summary>
    /// Loads a map manifest (JSON in Resources/Maps/) and instantiates pieces
    /// under a parent GameObject via prefab references resolved through
    /// MapPrefabRegistry. Currently client-local only — week 4+ moves spawn
    /// authority to the server.
    /// </summary>
    public static class MapLoader
    {
        public const string MapsResourceFolder = "Maps";
        public const string DefaultRegistryPath = "MapPrefabRegistry";

        public static GameObject Load(string mapName)
        {
            var json = Resources.Load<TextAsset>($"{MapsResourceFolder}/{mapName}");
            if (json == null)
            {
                Debug.LogError($"[MapLoader] Resources/{MapsResourceFolder}/{mapName}.json missing.");
                return null;
            }

            var registry = Resources.Load<MapPrefabRegistry>(DefaultRegistryPath);
            if (registry == null)
            {
                Debug.LogError($"[MapLoader] Resources/{DefaultRegistryPath}.asset missing — run `MmoGame > Setup Map Registry`.");
                return null;
            }

            var manifest = JsonUtility.FromJson<MapManifest>(json.text);
            if (manifest == null || manifest.pieces == null)
            {
                Debug.LogError($"[MapLoader] Failed to parse {mapName}.json");
                return null;
            }

            var root = new GameObject($"[Map:{manifest.name ?? mapName}]");
            int spawned = 0, missing = 0;

            foreach (var piece in manifest.pieces)
            {
                var prefab = registry.Get(piece.prefab);
                if (prefab == null)
                {
                    Debug.LogWarning($"[MapLoader] Unknown piece '{piece.prefab}' — skipping.");
                    missing++;
                    continue;
                }

                var go = Object.Instantiate(prefab, root.transform);
                go.transform.localPosition = ToVec3(piece.position, Vector3.zero);
                go.transform.localRotation = Quaternion.Euler(ToVec3(piece.rotation, Vector3.zero));
                go.transform.localScale = ToVec3(piece.scale, Vector3.one);
                spawned++;
            }

            BakeNavMesh(root);
            Debug.Log($"[MapLoader] {manifest.name ?? mapName}: spawned {spawned}, missing {missing}, navmesh baked.");
            return root;
        }

        static void BakeNavMesh(GameObject root)
        {
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
        }

        static Vector3 ToVec3(float[] arr, Vector3 fallback)
        {
            if (arr == null || arr.Length < 3) return fallback;
            return new Vector3(arr[0], arr[1], arr[2]);
        }
    }
}
