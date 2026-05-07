using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

namespace MmoGame.Networking
{
    /// <summary>
    /// Starts FishNet in the right mode for the current process.
    /// Order of precedence:
    ///   1. Headless / batchmode build → server only.
    ///   2. MPPM virtual player (not the main editor) → client only,
    ///      connecting back to the main editor's host on 127.0.0.1.
    ///   3. CLI flag --connect &lt;ip&gt; → client only against that ip.
    ///   4. Default → host (server + local client).
    /// Idempotent: a second Launch() call on an already-running instance
    /// is a no-op (avoids the FishNet bind exception that fires when
    /// StartConnection is invoked twice on the same ServerManager).
    /// </summary>
    public class NetworkLauncher : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;
        const string LocalHost = "127.0.0.1";

        public NetworkManager NetworkManager { get; private set; }
        public bool IsHost { get; private set; }
        public bool IsServerOnly { get; private set; }
        public bool IsClientOnly { get; private set; }

        bool _launched;

        public void Launch(string remoteHost = LocalHost, ushort port = DefaultPort)
        {
            if (_launched)
            {
                Debug.Log("[NetworkLauncher] Already launched, skipping.");
                return;
            }
            _launched = true;

            NetworkManager = FindFirstObjectByType<NetworkManager>();
            if (NetworkManager == null)
            {
                Debug.LogError("[NetworkLauncher] No NetworkManager in scene. Run `MmoGame > Setup Network` first.");
                return;
            }

            var tugboat = NetworkManager.GetComponent<Tugboat>();
            if (tugboat == null)
            {
                Debug.LogError("[NetworkLauncher] NetworkManager is missing a Tugboat transport.");
                return;
            }

            tugboat.SetPort(port);

            string[] args = System.Environment.GetCommandLineArgs();
            string connectFlag = ParseFlagValue(args, "--connect");
            string clientTarget = !string.IsNullOrEmpty(connectFlag) ? connectFlag : LocalHost;

            if (Application.isBatchMode)
            {
                IsServerOnly = true;
                NetworkManager.ServerManager.StartConnection();
                Debug.Log($"[NetworkLauncher] Dedicated server listening on :{port}");
                return;
            }

#if UNITY_EDITOR
            if (!CurrentPlayer.IsMainEditor)
            {
                IsClientOnly = true;
                tugboat.SetClientAddress(clientTarget);
                NetworkManager.ClientManager.StartConnection();
                Debug.Log($"[NetworkLauncher] MPPM virtual player → client connecting to {clientTarget}:{port}");
                return;
            }
#endif

            if (!string.IsNullOrEmpty(connectFlag))
            {
                IsClientOnly = true;
                tugboat.SetClientAddress(connectFlag);
                NetworkManager.ClientManager.StartConnection();
                Debug.Log($"[NetworkLauncher] Client connecting to {connectFlag}:{port}");
                return;
            }

            IsHost = true;
            tugboat.SetClientAddress(remoteHost);
            NetworkManager.ServerManager.StartConnection();
            NetworkManager.ClientManager.StartConnection();
            Debug.Log($"[NetworkLauncher] Host started on :{port} (server + local client)");
        }

        static string ParseFlagValue(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }
    }
}
