using System.Collections.Generic;
using System.Linq;
using MmoGame.World;
using UnityEditor;
using UnityEngine;

namespace MmoGame.Editor
{
    /// <summary>
    /// Pre-flight validation for map JSON manifests in Resources/Maps/.
    /// Catches problems at edit time so the author doesn't burn a Play
    /// session to discover a typo. Three classes of check:
    ///   - registry resolution: every prefab name exists
    ///   - sanity: Y range, scale, step values
    ///   - overlap: AABB volume overlap between non-ground pieces (uses
    ///     bounds extracted by SyntyCatalogScanner)
    /// </summary>
    public static class MapValidator
    {
        const string MapsFolder = "Assets/Resources/Maps";
        const float YSanityRange = 50f;
        const float OverlapThreshold = 0.5f;

        // Categories whose pieces are expected to overlap or stack — skipped for overlap checks.
        static readonly string[] OverlapWhitelistPrefixes = { "env_tile_", "env_path_", "env_grass" };

        // Pairs of prefixes whose mutual overlap is intentional (statue on base, chimney
        // through roof, sign mounted on wall, banner on tower, etc). Pair order doesn't
        // matter — we test both directions.
        static readonly (string, string)[] OverlapWhitelistPairs =
        {
            ("prop_statue_", "prop_statue_base"),
            ("bld_house_chimney_", "bld_house_room_top_"),
            ("prop_shop_sign_", "bld_house_door_"),
            ("prop_shop_sign_", "bld_house_room_"),
            ("prop_banner_",    "bld_castle_wall_"),
            ("prop_banner_",    "bld_house_tower_"),
            ("bld_house_tower_", "bld_castle_wall_"),
            ("bld_church_tower_", "bld_church_room_"),
            ("bld_church_extension_", "bld_church_room_"),
        };

        [MenuItem("MmoGame/Validate Maps")]
        public static void ValidateAll()
        {
            var registry = AssetDatabase.LoadAssetAtPath<MapPrefabRegistry>("Assets/Resources/MapPrefabRegistry.asset");
            if (registry == null)
            {
                Debug.LogError("[MapValidator] Registry missing — run `MmoGame > Rebuild Synty Catalog` first.");
                return;
            }
            var bounds = BuildBoundsLookup();

            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { MapsFolder });
            int total = 0, errored = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json")) continue;
                total++;
                if (!ValidateOne(path, registry, bounds)) errored++;
            }

            if (total == 0) Debug.LogWarning($"[MapValidator] No JSON files found under {MapsFolder}.");
            else Debug.Log($"[MapValidator] Validated {total} map(s), {errored} with issues.");
        }

        static bool ValidateOne(string path, MapPrefabRegistry registry, Dictionary<string, Vector3> bounds)
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            MapManifest manifest;
            try { manifest = JsonUtility.FromJson<MapManifest>(ta.text); }
            catch (System.Exception e) { Debug.LogError($"[MapValidator] {path}: parse error — {e.Message}"); return false; }

            if (manifest == null) { Debug.LogError($"[MapValidator] {path}: empty manifest"); return false; }

            int errors = 0, warnings = 0;
            var aabbs = new List<(string name, Vector3 center, Vector3 size)>();

            // pieces
            for (int i = 0; i < (manifest.pieces?.Length ?? 0); i++)
            {
                var p = manifest.pieces[i];
                var label = $"{path}#pieces[{i}] '{p.prefab}'";
                if (registry.Get(p.prefab) == null)
                {
                    Debug.LogError($"[MapValidator] {label}: unknown prefab name");
                    errors++; continue;
                }
                var pos = ToVec3(p.position, Vector3.zero);
                if (Mathf.Abs(pos.y) > YSanityRange)
                {
                    Debug.LogWarning($"[MapValidator] {label}: y={pos.y:F1} outside ±{YSanityRange} sanity range");
                    warnings++;
                }
                if (bounds.TryGetValue(p.prefab, out var bsize) && !IsOverlapWhitelisted(p.prefab))
                {
                    var scale = ToVec3(p.scale, Vector3.one);
                    aabbs.Add((p.prefab, pos, Vector3.Scale(bsize, scale)));
                }
            }

            // tilings
            for (int i = 0; i < (manifest.tilings?.Length ?? 0); i++)
            {
                var t = manifest.tilings[i];
                var label = $"{path}#tilings[{i}] '{t.prefab}'";
                if (registry.Get(t.prefab) == null)
                {
                    Debug.LogError($"[MapValidator] {label}: unknown prefab name");
                    errors++; continue;
                }
                if (t.step == null || t.step.Length < 3 || (t.step[0] <= 0 && t.step[2] <= 0))
                {
                    Debug.LogWarning($"[MapValidator] {label}: step missing or all-zero on X+Z — single instance only");
                    warnings++;
                }
            }

            // overlap pass — O(N²) but maps stay small in practice
            for (int i = 0; i < aabbs.Count; i++)
            for (int j = i + 1; j < aabbs.Count; j++)
            {
                var a = aabbs[i]; var b = aabbs[j];
                if (IsPairWhitelisted(a.name, b.name)) continue;
                var overlap = OverlapVolume(a.center, a.size, b.center, b.size);
                var minVol = Mathf.Min(Volume(a.size), Volume(b.size));
                if (minVol > 1e-4f && overlap / minVol > OverlapThreshold)
                {
                    Debug.LogWarning($"[MapValidator] {path}: overlap >{OverlapThreshold:P0} between '{a.name}' @ {a.center} and '{b.name}' @ {b.center}");
                    warnings++;
                }
            }

            if (errors == 0 && warnings == 0) Debug.Log($"[MapValidator] {path}: OK");
            else Debug.Log($"[MapValidator] {path}: {errors} error(s), {warnings} warning(s)");
            return errors == 0;
        }

        static Dictionary<string, Vector3> BuildBoundsLookup()
        {
            return SyntyCatalogScanner.ScanAll().ToDictionary(e => e.name, e => e.size);
        }

        static bool IsOverlapWhitelisted(string name) =>
            OverlapWhitelistPrefixes.Any(p => name.StartsWith(p));

        static bool IsPairWhitelisted(string a, string b) =>
            OverlapWhitelistPairs.Any(pair =>
                (a.StartsWith(pair.Item1) && b.StartsWith(pair.Item2)) ||
                (b.StartsWith(pair.Item1) && a.StartsWith(pair.Item2)));

        static Vector3 ToVec3(float[] arr, Vector3 fb) =>
            arr == null || arr.Length < 3 ? fb : new Vector3(arr[0], arr[1], arr[2]);

        static float Volume(Vector3 s) => s.x * s.y * s.z;

        static float OverlapVolume(Vector3 ca, Vector3 sa, Vector3 cb, Vector3 sb)
        {
            float x = Mathf.Max(0, Mathf.Min(ca.x + sa.x * 0.5f, cb.x + sb.x * 0.5f) - Mathf.Max(ca.x - sa.x * 0.5f, cb.x - sb.x * 0.5f));
            float y = Mathf.Max(0, Mathf.Min(ca.y + sa.y * 0.5f, cb.y + sb.y * 0.5f) - Mathf.Max(ca.y - sa.y * 0.5f, cb.y - sb.y * 0.5f));
            float z = Mathf.Max(0, Mathf.Min(ca.z + sa.z * 0.5f, cb.z + sb.z * 0.5f) - Mathf.Max(ca.z - sa.z * 0.5f, cb.z - sb.z * 0.5f));
            return x * y * z;
        }
    }
}
