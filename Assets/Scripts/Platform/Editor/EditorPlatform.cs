#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TacticalDuelist.Platform.Editor
{
    /// <summary>
    /// Editor / standalone mock platform for development and testing.
    /// All calls produce Debug.Log output. No external dependencies.
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

        public void Initialize()
        {
            Auth = new EditorAuth();
            Storage = new EditorStorage();
            Network = new EditorNetwork();
            Haptics = new EditorHaptics();
            Notifications = new EditorNotifications();
            DeepLinks = new EditorDeepLinks();
            Share = new EditorShare();
        }
    }

    #region Auth
    internal sealed class EditorAuth : IPlatformAuth
    {
        public UniTask<string> Authenticate()
        {
            Debug.Log("[EditorPlatform] Auth: returning mock token");
            return UniTask.FromResult("editor_mock_token_12345");
        }

        public string GetDisplayName() => "EditorPlayer";
        public string GetAvatarUrl() => string.Empty;
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
            new EditorWebSocketTransport(url);
    }

    internal sealed class EditorWebSocketTransport : IWebSocketTransport
    {
        private readonly string _url;

        public event Action<string, string> OnMessage;
        public event Action<string> OnError;
        public event Action OnDisconnected;

        public EditorWebSocketTransport(string url) => _url = url;

        public UniTask Connect()
        {
            Debug.Log($"[EditorPlatform] WS Connect → {_url}");
            return UniTask.CompletedTask;
        }

        public void Send(string eventName, string jsonPayload)
        {
            Debug.Log($"[EditorPlatform] WS Send: {eventName} → {jsonPayload}");
        }

        public void Disconnect()
        {
            Debug.Log("[EditorPlatform] WS Disconnect");
            OnDisconnected?.Invoke();
        }
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
        public void ShareReplay(string replayId, string message)
        {
            Debug.Log($"[EditorPlatform] ShareReplay: {replayId} — {message}");
        }

        public void InviteFriend(string matchId)
        {
            Debug.Log($"[EditorPlatform] InviteFriend: {matchId}");
        }
    }
    #endregion
}
#endif
