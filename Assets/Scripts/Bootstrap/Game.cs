using System;
using MmoGame.Backend;
using MmoGame.Networking;
using UnityEngine;

namespace MmoGame.Bootstrap
{
    /// <summary>
    /// Application entry point. Spawned automatically on game start via
    /// RuntimeInitializeOnLoadMethod — no scene setup or editor wiring
    /// required. Owns the Nakama client and (later) the FishNet network
    /// manager handle.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public NakamaClientService Nakama { get; private set; }
        public NetworkLauncher Net { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[Game]");
            DontDestroyOnLoad(go);
            go.AddComponent<NetworkLauncher>();
            go.AddComponent<Game>();
        }

        void Awake()
        {
            Instance = this;
            Net = GetComponent<NetworkLauncher>();
            Debug.Log($"[Game] Bootstrap on {Application.platform}, version {Application.version}");
            _ = StartupAsync();
        }

        async System.Threading.Tasks.Task StartupAsync()
        {
            Nakama = new NakamaClientService();
            var config = NakamaClientService.LoadConfig();
            try
            {
                var session = await Nakama.AuthenticateAsync(config);
                Debug.Log($"[Game] Authenticated as {session.UserId} (created={session.Created}) → {config.scheme}://{config.host}:{config.port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Game] Nakama auth failed: {e.GetType().Name}: {e.Message}");
                return;
            }

            Net.Launch();
        }
    }
}
