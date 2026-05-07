using MmoGame.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MmoGame.Editor
{
    /// <summary>
    /// Edit-time map instantiation — lets the author iterate JSON ↔ visual
    /// without entering Play mode (cycle ~5s vs ~30s through a Play loop).
    /// AI drivers invoke <see cref="Preview(string)"/> via MCP execute_code;
    /// human drivers use the per-map menu items.
    /// </summary>
    public static class MapPreview
    {
        public const string PreviewRootPrefix = "[MapPreview]";

        public static void Preview(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
            {
                Debug.LogError("[MapPreview] mapName is null/empty.");
                return;
            }

            // Resources.Load caches TextAssets aggressively; without this the preview
            // re-instantiates the previous JSON content even after the file was edited.
            var jsonPath = $"Assets/Resources/Maps/{mapName}.json";
            if (System.IO.File.Exists(jsonPath))
                AssetDatabase.ImportAsset(jsonPath, ImportAssetOptions.ForceUpdate);
            Resources.UnloadUnusedAssets();

            Clear();
            var root = MapLoader.Load(mapName, bakeNavMesh: false);
            if (root == null) return;

            root.name = $"{PreviewRootPrefix} {mapName}";
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[MapPreview] '{mapName}' instantiated for edit-mode preview. Use 'MmoGame > Clear Map Preview' to remove.");
        }

        [MenuItem("MmoGame/Preview Map/Selected JSON %#m")]
        static void PreviewSelected()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/Resources/Maps/") || !path.EndsWith(".json"))
            {
                Debug.LogWarning("[MapPreview] Select a .json file under Assets/Resources/Maps/ in the Project window first.");
                return;
            }
            Preview(System.IO.Path.GetFileNameWithoutExtension(path));
        }

        [MenuItem("MmoGame/Preview Map/Selected JSON %#m", true)]
        static bool PreviewSelectedValidate()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/Resources/Maps/") && path.EndsWith(".json");
        }

        [MenuItem("MmoGame/Clear Map Preview")]
        public static void Clear()
        {
            int removed = 0;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in roots)
            {
                if (go.name.StartsWith(PreviewRootPrefix))
                {
                    Object.DestroyImmediate(go);
                    removed++;
                }
            }
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log($"[MapPreview] Cleared {removed} preview root(s).");
            }
        }

        // Per-map convenience menu items. Add new entries as new maps land in Resources/Maps.
        [MenuItem("MmoGame/Preview Map/knights-camp")]
        static void PreviewKnightsCamp() => Preview("knights-camp");

        [MenuItem("MmoGame/Preview Map/prontera")]
        static void PreviewProntera() => Preview("prontera");
    }
}
