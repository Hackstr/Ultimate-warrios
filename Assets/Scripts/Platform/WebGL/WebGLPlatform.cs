#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TacticalDuelist.Platform.WebGL
{
    /// <summary>
    /// WebGL / Telegram Mini App platform implementation.
    /// Uses jslib interop for Telegram SDK and browser-native WebSocket.
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

        public void Initialize()
        {
            Auth = new WebGLAuth();
            Storage = new WebGLStorage();
            Network = new WebGLNetwork();
            Haptics = new WebGLHaptics();
            Notifications = new WebGLNotifications();
            DeepLinks = new WebGLDeepLinks();
            Share = new WebGLShare();
        }
    }

    #region Auth
    internal sealed class WebGLAuth : IPlatformAuth
    {
        [DllImport("__Internal")] private static extern string TMA_GetInitData();
        [DllImport("__Internal")] private static extern string TMA_GetUserDisplayName();
        [DllImport("__Internal")] private static extern string TMA_GetUserAvatarUrl();

        public UniTask<string> Authenticate()
        {
            var initData = TMA_GetInitData();
            return UniTask.FromResult(initData ?? string.Empty);
        }

        public string GetDisplayName() => TMA_GetUserDisplayName() ?? "Player";
        public string GetAvatarUrl() => TMA_GetUserAvatarUrl() ?? string.Empty;
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

    internal sealed class WebGLWebSocketTransport : IWebSocketTransport
    {
        private readonly string _url;

        public event Action<string, string> OnMessage;
        public event Action<string> OnError;
        public event Action OnDisconnected;

        public WebGLWebSocketTransport(string url) => _url = url;

        public UniTask Connect()
        {
            // TODO: Implement via jslib interop (WebSocket_Connect)
            Debug.Log($"[WebGL WS] Connecting to {_url}");
            return UniTask.CompletedTask;
        }

        public void Send(string eventName, string jsonPayload)
        {
            // TODO: Implement via jslib interop (WebSocket_Send)
            Debug.Log($"[WebGL WS] Send: {eventName}");
        }

        public void Disconnect()
        {
            // TODO: Implement via jslib interop (WebSocket_Close)
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

        public void ShareReplay(string replayId, string message)
        {
            var url = $"https://t.me/TacticalDuelistBot?startapp=replay_{replayId}";
            TMA_ShareUrl(url, message);
        }

        public void InviteFriend(string matchId)
        {
            var url = $"https://t.me/TacticalDuelistBot?startapp=invite_{matchId}";
            TMA_ShareUrl(url, "Join my match in Tactical Duelist!");
        }
    }
    #endregion
}
#endif
