using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace MmoGame.Networking
{
    /// <summary>
    /// Starts FishNet in the right mode for the current process.
    /// Editor / standalone client → host (server + local client).
    /// Headless server build (Application.isBatchMode) → server only.
    /// CLI flag --connect &lt;ip&gt; → client only, joins remote host.
    /// </summary>
    public class NetworkLauncher : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;

        public NetworkManager NetworkManager { get; private set; }
        public bool IsHost { get; private set; }
        public bool IsServerOnly { get; private set; }
        public bool IsClientOnly { get; private set; }

        public void Launch(string remoteHost = "127.0.0.1", ushort port = DefaultPort)
        {
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
            string connectTo = ParseFlagValue(args, "--connect");

            if (Application.isBatchMode)
            {
                IsServerOnly = true;
                NetworkManager.ServerManager.StartConnection();
                Debug.Log($"[NetworkLauncher] Dedicated server listening on :{port}");
            }
            else if (!string.IsNullOrEmpty(connectTo))
            {
                IsClientOnly = true;
                tugboat.SetClientAddress(connectTo);
                NetworkManager.ClientManager.StartConnection();
                Debug.Log($"[NetworkLauncher] Client connecting to {connectTo}:{port}");
            }
            else
            {
                IsHost = true;
                tugboat.SetClientAddress(remoteHost);
                NetworkManager.ServerManager.StartConnection();
                NetworkManager.ClientManager.StartConnection();
                Debug.Log($"[NetworkLauncher] Host started on :{port} (server + local client)");
            }
        }

        static string ParseFlagValue(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }
    }
}
