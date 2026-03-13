using System;

namespace TacticalDuelist.Platform
{
    /// <summary>
    /// Supported build targets for platform-specific behavior.
    /// </summary>
    public enum PlatformType
    {
        WebGL,
        Android,
        iOS,
        DesktopWeb
    }

    /// <summary>
    /// Root platform abstraction. One concrete implementation per build target.
    /// Injected at startup via PlatformBootstrap, accessed through ServiceLocator.
    /// </summary>
    public interface IPlatformService
    {
        PlatformType CurrentPlatform { get; }
        IPlatformAuth Auth { get; }
        IPlatformStorage Storage { get; }
        IPlatformNetwork Network { get; }
        IPlatformHaptics Haptics { get; }
        IPlatformNotifications Notifications { get; }
        IPlatformDeepLinks DeepLinks { get; }
        IPlatformShare Share { get; }

        void Initialize();
    }

    /// <summary>
    /// Platform-specific authentication (Telegram initData, Google Play, Apple GC, etc.).
    /// </summary>
    public interface IPlatformAuth
    {
        Cysharp.Threading.Tasks.UniTask<string> Authenticate();
        string GetDisplayName();
        string GetAvatarUrl();
    }

    /// <summary>
    /// Key-value storage abstraction. Backed by PlayerPrefs on most platforms.
    /// </summary>
    public interface IPlatformStorage
    {
        void Save(string key, string value);
        string Load(string key);
        void Delete(string key);
    }

    /// <summary>
    /// Factory for platform-appropriate WebSocket transports.
    /// </summary>
    public interface IPlatformNetwork
    {
        IWebSocketTransport CreateWebSocket(string url);
    }

    /// <summary>
    /// Haptic feedback. Supported on mobile; no-op on WebGL and desktop.
    /// </summary>
    public interface IPlatformHaptics
    {
        bool IsSupported { get; }
        void LightImpact();
        void MediumImpact();
        void HeavyImpact();
    }

    /// <summary>
    /// Local push notifications for re-engagement.
    /// </summary>
    public interface IPlatformNotifications
    {
        void ScheduleLocal(string title, string body, TimeSpan delay);
        void CancelAll();
    }

    /// <summary>
    /// Deep link handling for invite URLs, etc.
    /// </summary>
    public interface IPlatformDeepLinks
    {
        event Action<string> OnDeepLinkReceived;
        void OpenUrl(string url);
    }

    /// <summary>
    /// Friend invite through platform share sheets.
    /// </summary>
    public interface IPlatformShare
    {
        void InviteFriend(string matchId);
    }
}
