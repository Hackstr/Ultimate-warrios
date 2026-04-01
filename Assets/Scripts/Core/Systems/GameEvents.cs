using System;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Central static event hub. All game state changes flow through here.
    /// View layer subscribes in OnEnable, unsubscribes in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        #region Match Lifecycle

        public static event Action<MatchStartData> OnMatchStarted;
        public static event Action<MatchResult> OnMatchEnded;

        #endregion

        #region Round Lifecycle

        public static event Action<int> OnRoundStarted;
        public static event Action<RoundResult> OnRoundEnded;
        public static event Action<GamePhase> OnPhaseChanged;

        #endregion

        #region Step Resolution

        public static event Action<StepResult> OnStepResolved;

        #endregion

        #region Planning

        public static event Action<int> OnPlanningTimerTick;
        public static event Action OnPlanningTimeExpired;
        public static event Action<int, ActionType> OnActionQueued;
        public static event Action<int> OnActionUndone;

        #endregion

        #region Hero State

        public static event Action<int, bool> OnArmorChanged;
        public static event Action<int> OnHeroEliminated;

        #endregion

        #region Map Changes

        public static event Action<UnityEngine.Vector2Int[]> OnDangerZoneExpanded;
        public static event Action<UnityEngine.Vector2Int> OnWallDestroyed;

        #endregion

        #region Network

        public static event Action<string> OnNetworkConnected;
        public static event Action<string> OnNetworkDisconnected;
        public static event Action<string> OnNetworkError;

        #endregion

        #region Invoke Helpers

        public static void MatchStarted(MatchStartData data) => OnMatchStarted?.Invoke(data);
        public static void MatchEnded(MatchResult result) => OnMatchEnded?.Invoke(result);
        public static void RoundStarted(int round) => OnRoundStarted?.Invoke(round);
        public static void RoundEnded(RoundResult result) => OnRoundEnded?.Invoke(result);
        public static void PhaseChanged(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
        public static void StepResolved(StepResult result) => OnStepResolved?.Invoke(result);
        public static void PlanningTimerTick(int secondsRemaining) => OnPlanningTimerTick?.Invoke(secondsRemaining);
        public static void PlanningTimeExpired() => OnPlanningTimeExpired?.Invoke();
        public static void ActionQueued(int slotIndex, ActionType action) => OnActionQueued?.Invoke(slotIndex, action);
        public static void ActionUndone(int slotIndex) => OnActionUndone?.Invoke(slotIndex);
        public static void ArmorChanged(int playerIndex, bool hasArmor) => OnArmorChanged?.Invoke(playerIndex, hasArmor);
        public static void HeroEliminated(int playerIndex) => OnHeroEliminated?.Invoke(playerIndex);
        public static void DangerZoneExpanded(UnityEngine.Vector2Int[] tiles) => OnDangerZoneExpanded?.Invoke(tiles);
        public static void WallDestroyed(UnityEngine.Vector2Int pos) => OnWallDestroyed?.Invoke(pos);
        public static void NetworkConnected(string sessionId) => OnNetworkConnected?.Invoke(sessionId);
        public static void NetworkDisconnected(string reason) => OnNetworkDisconnected?.Invoke(reason);
        public static void NetworkError(string error) => OnNetworkError?.Invoke(error);

        #endregion

        #region Voice / Toast

        public static System.Action<string> ShowToast;

        #endregion

        #region Cleanup

        /// <summary>
        /// Clears all subscribers. Call on match end or scene unload to prevent memory leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnMatchStarted = null;
            OnMatchEnded = null;
            OnRoundStarted = null;
            OnRoundEnded = null;
            OnPhaseChanged = null;
            OnStepResolved = null;
            OnPlanningTimerTick = null;
            OnPlanningTimeExpired = null;
            OnActionQueued = null;
            OnActionUndone = null;
            OnArmorChanged = null;
            OnHeroEliminated = null;
            OnDangerZoneExpanded = null;
            OnWallDestroyed = null;
            OnNetworkConnected = null;
            OnNetworkDisconnected = null;
            OnNetworkError = null;
        }

        #endregion
    }

    /// <summary>
    /// Data passed when a match starts. Contains initial state for View layer setup.
    /// </summary>
    public class MatchStartData
    {
        public string MatchId;
        public Config.HeroConfig P1Hero;
        public Config.HeroConfig P2Hero;
        public Config.MapConfig Map;
        public UnityEngine.Vector2Int P1Spawn;
        public UnityEngine.Vector2Int P2Spawn;
        public Direction P1Facing;
        public Direction P2Facing;
        public int MaxRounds;
    }
}
