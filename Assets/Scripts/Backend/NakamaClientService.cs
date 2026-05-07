using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace MmoGame.Backend
{
    /// <summary>
    /// Wrapper around Nakama's IClient + session lifecycle. One instance per
    /// application, owned by the bootstrap Game object. Handles initial
    /// authenticate-by-device-id, exposes the live session to the rest of
    /// the codebase. Email/password and account linking land in week 5+.
    /// </summary>
    public class NakamaClientService
    {
        const string DeviceIdPlayerPrefKey = "mmogame.deviceId";

        public IClient Client { get; private set; }
        public ISession Session { get; private set; }
        public bool IsAuthenticated => Session != null && !Session.IsExpired;

        public async Task<ISession> AuthenticateAsync(NakamaConfig config)
        {
            Client = new Client(config.scheme, config.host, config.port, config.serverKey)
            {
                Timeout = 5
            };

            string deviceId = ResolveDeviceId();
            Session = await Client.AuthenticateDeviceAsync(deviceId);
            return Session;
        }

        public static NakamaConfig LoadConfig()
        {
            var asset = Resources.Load<TextAsset>("nakama-config");
            if (asset == null)
            {
                Debug.LogWarning("[Nakama] Resources/nakama-config.json missing — using defaults.");
                return new NakamaConfig();
            }
            return JsonUtility.FromJson<NakamaConfig>(asset.text);
        }

        static string ResolveDeviceId()
        {
            var systemId = SystemInfo.deviceUniqueIdentifier;
            if (!string.IsNullOrEmpty(systemId) && systemId != SystemInfo.unsupportedIdentifier)
                return systemId;

            var stored = PlayerPrefs.GetString(DeviceIdPlayerPrefKey, null);
            if (!string.IsNullOrEmpty(stored)) return stored;

            var fresh = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(DeviceIdPlayerPrefKey, fresh);
            PlayerPrefs.Save();
            return fresh;
        }
    }
}
