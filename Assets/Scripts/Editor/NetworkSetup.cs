using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using MmoGame.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MmoGame.Editor
{
    /// <summary>
    /// One-shot scene & prefab wiring. Designed to be called either via the
    /// menu or programmatically (e.g. by Claude through MCP execute_menu_item).
    /// Idempotent — safe to run multiple times; reuses existing assets where
    /// possible and only patches what's missing.
    /// </summary>
    public static class NetworkSetup
    {
        const string PlayerPrefabPath = "Assets/Content/Prefabs/Player.prefab";
        const string PlayerFolder = "Assets/Content/Prefabs";
        const string ContentRoot = "Assets/Content";
        const string DefaultPrefabsAssetPath = "Assets/DefaultPrefabObjects.asset";

        [MenuItem("MmoGame/Setup Network")]
        public static void Run()
        {
            EnsureFolders();
            var playerPrefab = EnsurePlayerPrefab();
            EnsureSceneNetwork(playerPrefab);
            RegisterDefaultPrefab(playerPrefab);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[NetworkSetup] Done. Press Play to host; second instance can join via --connect 127.0.0.1.");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ContentRoot))
                AssetDatabase.CreateFolder("Assets", "Content");
            if (!AssetDatabase.IsValidFolder(PlayerFolder))
                AssetDatabase.CreateFolder(ContentRoot, "Prefabs");
        }

        static GameObject EnsurePlayerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null) return existing;

            // Build a capsule placeholder — Synty character swap-in is week 3+
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Player";
            // Strip the auto-added collider's physics — we just want a visible mesh
            capsule.GetComponent<CapsuleCollider>().isTrigger = true;

            var netObj = capsule.AddComponent<NetworkObject>();
            var netTransform = capsule.AddComponent<NetworkTransform>();
            // NetworkTransform defaults are fine for a placeholder; tighten later
            capsule.AddComponent<PlayerController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(capsule, PlayerPrefabPath);
            Object.DestroyImmediate(capsule);
            return prefab;
        }

        static void EnsureSceneNetwork(GameObject playerPrefab)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[NetworkSetup] No active scene. Open SampleScene and re-run.");
                return;
            }

            var existing = Object.FindFirstObjectByType<NetworkManager>();
            if (existing == null)
            {
                var go = new GameObject("[NetworkManager]");
                var nm = go.AddComponent<NetworkManager>();
                go.AddComponent<Tugboat>();
                existing = nm;
            }

            // Make sure Tugboat is attached
            if (existing.GetComponent<Tugboat>() == null)
                existing.gameObject.AddComponent<Tugboat>();

            var spawner = existing.GetComponent<PlayerSpawner>()
                          ?? existing.gameObject.AddComponent<PlayerSpawner>();
            var netObj = playerPrefab.GetComponent<NetworkObject>();
            if (netObj != null) spawner.SetPlayerPrefab(netObj);
            EditorUtility.SetDirty(spawner);

            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void RegisterDefaultPrefab(GameObject playerPrefab)
        {
            var defaultPrefabs = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(DefaultPrefabsAssetPath);
            if (defaultPrefabs == null)
            {
                Debug.LogWarning("[NetworkSetup] DefaultPrefabObjects.asset missing — FishNet should create it on next domain reload.");
                return;
            }

            var netObj = playerPrefab.GetComponent<NetworkObject>();
            if (netObj == null) return;

            // AddObject is the public API on PrefabObjects; safe to call repeatedly (it dedupes).
            defaultPrefabs.AddObject(netObj, true);
            EditorUtility.SetDirty(defaultPrefabs);
        }
    }
}
