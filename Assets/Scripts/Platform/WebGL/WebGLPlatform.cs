#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TacticalDuelist.Platform.WebGL
{
    /// <summary>
    /// WebGL / Telegram Mini App platform implementation.
    /// Uses jslib interop for Telegram SDK and browser-native Socket.IO.
    /// </summary>
    public sealed class WebGLPlatform : IPlatformService
    {
        public PlatformType CurrentPlatform => PlatformType.WebGL;
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
            Auth = new WebGLAuth();
            Storage = new WebGLStorage();
            Network = new WebGLNetwork();
            Haptics = new WebGLHaptics();
            Notifications = new WebGLNotifications();
            DeepLinks = new WebGLDeepLinks();
            Share = new WebGLShare();
#if UNITY_WEBGL && !UNITY_EDITOR
            var bc = new WebGLBlockchain();
            bc.Initialize();
            Blockchain = bc;
#endif
        }
    }

    #region Auth
    internal sealed class WebGLAuth : IPlatformAuth
    {
        [DllImport("__Internal")] private static extern string TMA_GetInitData();
        [DllImport("__Internal")] private static extern string TMA_GetUserDisplayName();
        [DllImport("__Internal")] private static extern string TMA_GetUserAvatarUrl();

        private string _cachedToken;

        /// <summary>
        /// Retrieves Telegram initData, sends it to server /auth/telegram,
        /// and returns the JWT token for Socket.IO authentication.
        /// </summary>
        public async UniTask<string> Authenticate()
        {
            if (!string.IsNullOrEmpty(_cachedToken))
                return _cachedToken;

            var initData = TMA_GetInitData();
            if (string.IsNullOrEmpty(initData))
            {
                Debug.LogWarning("[WebGLAuth] No initData from Telegram, auth will fail");
                return string.Empty;
            }

            var serverUrl = WebGLConfig.GetServerUrl();
            var body = $"{{\"initData\":\"{EscapeJson(initData)}\"}}";
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);

            using var req = new UnityWebRequest($"{serverUrl}/auth/telegram", "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;

            try
            {
                await req.SendWebRequest().ToUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLAuth] Auth request failed: {ex.Message}");
                return string.Empty;
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[WebGLAuth] Auth failed ({req.responseCode}): {req.downloadHandler.text}");
                return string.Empty;
            }

            var response = JsonUtility.FromJson<TelegramAuthResponse>(req.downloadHandler.text);
            _cachedToken = response.token;
            Debug.Log($"[WebGLAuth] Authenticated (playerId={response.playerId})");
            return _cachedToken;
        }

        public string GetDisplayName() => TMA_GetUserDisplayName() ?? "Player";
        public string GetAvatarUrl() => TMA_GetUserAvatarUrl() ?? string.Empty;

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        [Serializable]
        private class TelegramAuthResponse
        {
            public string token;
            public string playerId;
        }
    }
    #endregion

    #region Storage
    internal sealed class WebGLStorage : IPlatformStorage
    {
        public void Save(string key, string value) => PlayerPrefs.SetString(key, value);
        public string Load(string key) => PlayerPrefs.GetString(key, string.Empty);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
    }
    #endregion

    #region Network
    internal sealed class WebGLNetwork : IPlatformNetwork
    {
        public IWebSocketTransport CreateWebSocket(string url) =>
            new WebGLWebSocketTransport(url);
    }

    /// <summary>
    /// WebGL Socket.IO transport. Delegates to SocketIOPlugin.jslib which uses
    /// the browser's socket.io-client library. Messages are routed back through
    /// WebGLSocketReceiver MonoBehaviour via SendMessage.
    /// </summary>
    internal sealed class WebGLWebSocketTransport : IWebSocketTransport
    {
        [DllImport("__Internal")] private static extern void SocketIO_Connect(string url, string authToken);
        [DllImport("__Internal")] private static extern void SocketIO_Send(string eventName, string payload);
        [DllImport("__Internal")] private static extern void SocketIO_Disconnect();

        private readonly string _url;
        private string _authToken;
        private UniTaskCompletionSource _connectTcs;

        public event Action<string, string> OnMessage;
        public event Action<string> OnError;
        public event Action OnDisconnected;

        public WebGLWebSocketTransport(string url) => _url = url;

        public void SetAuthToken(string token) => _authToken = token;

        public UniTask Connect()
        {
            WebGLSocketReceiver.SetTransport(this);
            _connectTcs = new UniTaskCompletionSource();
            SocketIO_Connect(_url, _authToken ?? "");
            return _connectTcs.Task;
        }

        public void Send(string eventName, string jsonPayload)
        {
            SocketIO_Send(eventName, jsonPayload);
        }

        public void Disconnect()
        {
            SocketIO_Disconnect();
        }

        internal void HandleConnected()
        {
            Debug.Log("[WebGL WS] Connected");
            _connectTcs?.TrySetResult();
        }

        internal void HandleMessage(string eventName, string payload)
        {
            OnMessage?.Invoke(eventName, payload);
        }

        internal void HandleError(string error)
        {
            Debug.LogError($"[WebGL WS] Error: {error}");
            OnError?.Invoke(error);
            _connectTcs?.TrySetException(new Exception(error));
        }

        internal void HandleDisconnected(string reason)
        {
            Debug.Log($"[WebGL WS] Disconnected: {reason}");
            OnDisconnected?.Invoke();
        }
    }
    #endregion

    #region Haptics
    internal sealed class WebGLHaptics : IPlatformHaptics
    {
        [DllImport("__Internal")] private static extern void TMA_HapticImpact(string style);

        public bool IsSupported => true;
        public void LightImpact() => TMA_HapticImpact("light");
        public void MediumImpact() => TMA_HapticImpact("medium");
        public void HeavyImpact() => TMA_HapticImpact("heavy");
    }
    #endregion

    #region Notifications (not supported on WebGL)
    internal sealed class WebGLNotifications : IPlatformNotifications
    {
        public void ScheduleLocal(string title, string body, TimeSpan delay) { }
        public void CancelAll() { }
    }
    #endregion

    #region DeepLinks
    internal sealed class WebGLDeepLinks : IPlatformDeepLinks
    {
        public event Action<string> OnDeepLinkReceived;

        public void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

        internal void RaiseDeepLink(string url) => OnDeepLinkReceived?.Invoke(url);
    }
    #endregion

    #region Share
    internal sealed class WebGLShare : IPlatformShare
    {
        [DllImport("__Internal")] private static extern void TMA_ShareUrl(string url, string text);

        public void InviteFriend(string matchId)
        {
            var url = $"https://t.me/TacticalDuelistBot?startapp=invite_{matchId}";
            TMA_ShareUrl(url, "Join my match in Tactical Duelist!");
        }
    }
    #endregion
}
#endif
