namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Centralized constants for game flow timing, limits, and thresholds.
    /// </summary>
    public static class GameConstants
    {
        // ── Timeouts ──
        public const float WaitingTimeoutSec = 90f;
        public const float RejoinTimeoutSec = 5f;
        public const int MaxRejoinAttempts = 3;

        // ── Planning ──
        public const float DefaultTimeLimit = 30f;

        // ── Reconnection ──
        public const int ReconnectMaxAttempts = 5;
    }
}
