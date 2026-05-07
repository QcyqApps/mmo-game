using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MmoGame.World
{
    /// <summary>
    /// Loads a map manifest (JSON in Resources/Maps/) and instantiates pieces
    /// + tilings under a parent GameObject via prefab references resolved
    /// through MapPrefabRegistry. Currently client-local only — week 4+
    /// moves spawn authority to the server.
    /// </summary>
    public static class MapLoader
    {
        public const string MapsResourceFolder = "Maps";
        public const string DefaultRegistryPath = "MapPrefabRegistry";

        public static GameObject Load(string mapName, bool bakeNavMesh = true)
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
                Debug.LogError($"[MapLoader] Resources/{DefaultRegistryPath}.asset missing — run `MmoGame > Rebuild Synty Catalog`.");
                return null;
            }

            var manifest = JsonUtility.FromJson<MapManifest>(json.text);
            if (manifest == null)
            {
                Debug.LogError($"[MapLoader] Failed to parse {mapName}.json");
                return null;
            }

            var root = new GameObject($"[Map:{manifest.name ?? mapName}]");
            var groups = new Dictionary<string, Transform>();
            int spawned = 0, missing = 0;

            if (manifest.pieces != null)
                for (int i = 0; i < manifest.pieces.Length; i++)
                    SpawnPiece(manifest.pieces[i], i, root.transform, groups, registry, ref spawned, ref missing);

            if (manifest.tilings != null)
                for (int i = 0; i < manifest.tilings.Length; i++)
                    SpawnTiling(manifest.tilings[i], i, root.transform, groups, registry, ref spawned, ref missing);

            if (bakeNavMesh) BakeNavMesh(root);

            Debug.Log($"[MapLoader] {manifest.name ?? mapName}: spawned {spawned}, missing {missing}{(bakeNavMesh ? ", navmesh baked" : "")}.");
            return root;
        }

        static void SpawnPiece(MapPiece piece, int pieceIndex, Transform root, Dictionary<string, Transform> groups,
                                MapPrefabRegistry registry, ref int spawned, ref int missing)
        {
            var prefab = registry.Get(piece.prefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[MapLoader] Unknown piece '{piece.prefab}' — skipping.");
                missing++;
                return;
            }
            var parent = ResolveGroup(piece.parent, root, groups);
            var go = Object.Instantiate(prefab, parent);
            go.transform.localPosition = ToVec3(piece.position, Vector3.zero);
            go.transform.localRotation = Quaternion.Euler(ToVec3(piece.rotation, Vector3.zero));
            go.transform.localScale = ToVec3(piece.scale, Vector3.one);
            var marker = go.AddComponent<MapPieceMarker>();
            marker.kind = MapMarkerKind.Piece;
            marker.pieceIndex = pieceIndex;
            marker.prefabName = piece.prefab;
            marker.groupName = piece.parent;
            spawned++;
        }

        static void SpawnTiling(MapTiling tiling, int tilingIndex, Transform root, Dictionary<string, Transform> groups,
                                 MapPrefabRegistry registry, ref int spawned, ref int missing)
        {
            var prefab = registry.Get(tiling.prefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[MapLoader] Unknown tiling piece '{tiling.prefab}' — skipping {tiling.note ?? "tiling"}.");
                missing++;
                return;
            }
            var min = ToVec3(tiling.min, Vector3.zero);
            var max = ToVec3(tiling.max, min);
            var step = ToVec3(tiling.step, Vector3.one);
            var rot = Quaternion.Euler(ToVec3(tiling.rotation, Vector3.zero));
            var parent = ResolveGroup(tiling.parent, root, groups);

            // Steps <=0 collapse that axis to a single value.
            float sx = step.x > 1e-4f ? step.x : (max.x - min.x + 1f);
            float sy = step.y > 1e-4f ? step.y : (max.y - min.y + 1f);
            float sz = step.z > 1e-4f ? step.z : (max.z - min.z + 1f);

            const float EPS = 1e-4f;
            int ix = 0, iy, iz;
            for (float x = min.x; x <= max.x + EPS; x += sx, ix++)
            {
                iy = 0;
                for (float y = min.y; y <= max.y + EPS; y += sy, iy++)
                {
                    iz = 0;
                    for (float z = min.z; z <= max.z + EPS; z += sz, iz++)
                    {
                        var go = Object.Instantiate(prefab, parent);
                        go.transform.localPosition = new Vector3(x, y, z);
                        go.transform.localRotation = rot;
                        var marker = go.AddComponent<MapPieceMarker>();
                        marker.kind = MapMarkerKind.Tiling;
                        marker.tilingIndex = tilingIndex;
                        marker.tilingIx = ix;
                        marker.tilingIy = iy;
                        marker.tilingIz = iz;
                        marker.prefabName = tiling.prefab;
                        marker.groupName = tiling.parent;
                        spawned++;
                    }
                }
            }
        }

        static Transform ResolveGroup(string name, Transform root, Dictionary<string, Transform> cache)
        {
            if (string.IsNullOrEmpty(name)) return root;
            if (cache.TryGetValue(name, out var existing)) return existing;
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            cache[name] = go.transform;
            return go.transform;
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
