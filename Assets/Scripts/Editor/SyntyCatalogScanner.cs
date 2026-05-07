using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MmoGame.World;
using UnityEditor;
using UnityEngine;

namespace MmoGame.Editor
{
    /// <summary>
    /// Scans Assets/Synty/**/*.prefab, derives a logical name + AABB bounds
    /// per piece, and emits two artifacts:
    ///   1. Assets/Resources/MapPrefabRegistry.asset — auto-populated lookup
    ///      consumed by MapLoader at runtime.
    ///   2. Assets/Docs/maps/synty-catalog.md — human/AI readable index with
    ///      size, center offset, asset path. Authors (and the map-author
    ///      subagent) read this BEFORE writing a map JSON, so they pick the
    ///      right piece for the right footprint.
    /// Idempotent — overwrites both artifacts on every run.
    /// </summary>
    public static class SyntyCatalogScanner
    {
        const string SyntyRoot = "Assets/Synty";
        const string RegistryPath = "Assets/Resources/MapPrefabRegistry.asset";
        const string CatalogDocPath = "Assets/Docs/maps/synty-catalog.md";
        const string ResourcesFolder = "Assets/Resources";
        const string DocsRoot = "Assets/Docs";
        const string DocsMapsFolder = "Assets/Docs/maps";

        // Subfolders under each Synty pack's Prefabs/ directory that we surface
        // as map pieces. Characters + Weapons live elsewhere in the pipeline.
        static readonly string[] CategoryFolders = { "Buildings", "Environments", "Props" };

        public class CatalogEntry
        {
            public string name;
            public string category;
            public Vector3 size;
            public Vector3 centerOffset;
            public string path;
            public GameObject prefab;
        }

        [MenuItem("MmoGame/Rebuild Synty Catalog")]
        public static void Rebuild()
        {
            var entries = ScanAll();
            EnsureFolders();
            WriteRegistry(entries);
            WriteCatalogDoc(entries);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SyntyCatalog] Rebuilt: {entries.Count} entries → {RegistryPath} + {CatalogDocPath}");
        }

        public static List<CatalogEntry> ScanAll()
        {
            var result = new List<CatalogEntry>();
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { SyntyRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var category = GetCategory(path);
                if (category == null) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;

                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                result.Add(new CatalogEntry
                {
                    name = ToLogicalName(prefab.name),
                    category = category.ToLower(),
                    size = bounds.size,
                    centerOffset = bounds.center - prefab.transform.position,
                    path = path,
                    prefab = prefab,
                });
            }
            return result.OrderBy(e => e.category).ThenBy(e => e.name).ToList();
        }

        static string GetCategory(string path)
        {
            // Path: Assets/Synty/<Pack>/Prefabs/<Category>/.../<file>.prefab
            var idx = path.IndexOf("/Prefabs/");
            if (idx < 0) return null;
            var rest = path.Substring(idx + "/Prefabs/".Length);
            var slash = rest.IndexOf('/');
            if (slash < 0) return null;
            var cat = rest.Substring(0, slash);
            return CategoryFolders.Contains(cat) ? cat : null;
        }

        static string ToLogicalName(string prefabName)
        {
            // SM_Bld_Tent_01 → bld_tent_01
            // SM_Env_GroundMound_01 → env_ground_mound_01
            var stripped = Regex.Replace(prefabName, @"^SM_", "");
            var snake = Regex.Replace(stripped, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
            return snake;
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(DocsRoot))
                AssetDatabase.CreateFolder("Assets", "Docs");
            if (!AssetDatabase.IsValidFolder(DocsMapsFolder))
                AssetDatabase.CreateFolder(DocsRoot, "maps");
        }

        static void WriteRegistry(List<CatalogEntry> entries)
        {
            var registry = AssetDatabase.LoadAssetAtPath<MapPrefabRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<MapPrefabRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            registry.entries = entries
                .Select(e => new MapPrefabRegistry.Entry { name = e.name, prefab = e.prefab })
                .ToArray();
            EditorUtility.SetDirty(registry);
        }

        static void WriteCatalogDoc(List<CatalogEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Synty Catalog (auto-generated)");
            sb.AppendLine();
            sb.AppendLine($"Generated by `MmoGame > Rebuild Synty Catalog`. **{entries.Count} entries.**");
            sb.AppendLine();
            sb.AppendLine("Authors and the `map-author` subagent reference logical names from this");
            sb.AppendLine("catalog when filling map JSON `pieces[].prefab` or `tilings[].prefab` fields.");
            sb.AppendLine("Size = world-space AABB across all renderers (use it for grid steps and");
            sb.AppendLine("overlap math). Center offset = `bounds.center − prefab.transform.position`");
            sb.AppendLine("(non-zero when a model's pivot isn't at its visual center).");
            sb.AppendLine();
            sb.AppendLine("Re-run the menu after importing or updating any Synty pack.");
            sb.AppendLine();

            foreach (var category in entries.Select(e => e.category).Distinct().OrderBy(c => c))
            {
                var inCat = entries.Where(e => e.category == category).ToList();
                sb.AppendLine($"## {category} ({inCat.Count})");
                sb.AppendLine();
                sb.AppendLine("| name | size (x, y, z) | center offset | path |");
                sb.AppendLine("|------|----------------|---------------|------|");
                foreach (var e in inCat)
                    sb.AppendLine($"| `{e.name}` | {Fmt(e.size)} | {Fmt(e.centerOffset)} | `{e.path}` |");
                sb.AppendLine();
            }

            File.WriteAllText(CatalogDocPath, sb.ToString());
            AssetDatabase.ImportAsset(CatalogDocPath);
        }

        static string Fmt(Vector3 v) => $"{v.x:F2}, {v.y:F2}, {v.z:F2}";
    }
}
