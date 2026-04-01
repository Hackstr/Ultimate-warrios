#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TacticalDuelist.Platform.Editor
{
    /// <summary>
    /// Editor / standalone platform for development and testing.
    /// Auth hits the server's POST /auth/dev endpoint.
    /// WebSocket uses real Engine.IO/Socket.IO transport.
    /// </summary>
    public sealed class EditorPlatform : IPlatformService
    {
        public PlatformType CurrentPlatform => PlatformType.DesktopWeb;
        public IPlatformAuth Auth { get; private set; }
        public IPlatformStorage Storage { get; private set; }
        public IPlatformNetwork Network { get; private set; }
        public IPlatformHaptics Haptics { get; private set; }
        public IPlatformNotifications Notifications { get; private set; }
        public IPlatformDeepLinks DeepLinks { get; private set; }
        public IPlatformShare Share { get; private set; }
        public IBlockchainService Blockchain { get; private set; }

        public void Initialize()
        {
            Auth = new EditorAuth();
            Storage = new EditorStorage();
            Network = new EditorNetwork();
            Haptics = new EditorHaptics();
            Notifications = new EditorNotifications();
            DeepLinks = new EditorDeepLinks();
            Share = new EditorShare();
            Blockchain = new EditorBlockchain();
        }
    }

    #region Auth
    internal sealed class EditorAuth : IPlatformAuth
    {
        private const string DevAuthUrl = "http://localhost:3000/auth/dev";
        private string _cachedToken;

        public async UniTask<string> Authenticate()
        {
            if (!string.IsNullOrEmpty(_cachedToken))
                return _cachedToken;

            var username = $"editor_{SystemInfo.deviceUniqueIdentifier[..8]}";
            var body = $"{{\"username\":\"{username}\"}}";
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);

            using var req = new UnityWebRequest(DevAuthUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;

            try
            {
                await req.SendWebRequest().ToUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorAuth] Server unreachable ({ex.Message}), using mock token");
                return "editor_mock_token_12345";
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[EditorAuth] Auth failed ({req.responseCode}): {req.downloadHandler.text}, using mock token");
                return "editor_mock_token_12345";
            }

            var response = JsonUtility.FromJson<DevAuthResponse>(req.downloadHandler.text);
            _cachedToken = response.token;
            Debug.Log($"[EditorAuth] Authenticated as {username} (playerId={response.playerId})");
            return _cachedToken;
        }

        public string GetDisplayName() => "EditorPlayer";
        public string GetAvatarUrl() => string.Empty;

        [Serializable]
        private class DevAuthResponse
        {
            public string token;
            public string playerId;
        }
    }
    #endregion

    #region Storage
    internal sealed class EditorStorage : IPlatformStorage
    {
        public void Save(string key, string value) => PlayerPrefs.SetString(key, value);
        public string Load(string key) => PlayerPrefs.GetString(key, string.Empty);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
    }
    #endregion

    #region Network
    internal sealed class EditorNetwork : IPlatformNetwork
    {
        public IWebSocketTransport CreateWebSocket(string url) =>
            new EditorSocketIOTransport(url);
    }
    #endregion

    #region Haptics (no-op in editor)
    internal sealed class EditorHaptics : IPlatformHaptics
    {
        public bool IsSupported => false;
        public void LightImpact() => Debug.Log("[EditorPlatform] Haptic: light");
        public void MediumImpact() => Debug.Log("[EditorPlatform] Haptic: medium");
        public void HeavyImpact() => Debug.Log("[EditorPlatform] Haptic: heavy");
    }
    #endregion

    #region Notifications (mock)
    internal sealed class EditorNotifications : IPlatformNotifications
    {
        public void ScheduleLocal(string title, string body, TimeSpan delay)
        {
            Debug.Log($"[EditorPlatform] Notification scheduled: \"{title}\" in {delay.TotalSeconds}s");
        }

        public void CancelAll() => Debug.Log("[EditorPlatform] All notifications cancelled");
    }
    #endregion

    #region DeepLinks
    internal sealed class EditorDeepLinks : IPlatformDeepLinks
    {
        public event Action<string> OnDeepLinkReceived;

        public void OpenUrl(string url)
        {
            Debug.Log($"[EditorPlatform] OpenURL: {url}");
            Application.OpenURL(url);
        }

        internal void SimulateDeepLink(string url) => OnDeepLinkReceived?.Invoke(url);
    }
    #endregion

    #region Share
    internal sealed class EditorShare : IPlatformShare
    {
        public void InviteFriend(string matchId)
        {
            Debug.Log($"[EditorPlatform] InviteFriend: {matchId}");
        }
    }
    #endregion
}
#endif
