using System;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;
using UnityEngine;

namespace TacticalDuelist.Networking
{
    /// <summary>
    /// Bridges the Socket.IO client and the game event system.
    /// Translates outgoing game actions into server events,
    /// and incoming server messages into GameEvents calls.
    /// </summary>
    public sealed class MatchNetworkController : IDisposable
    {
        #region Constants

        private const string EventMatchFind = "match:find";
        private const string EventMatchCancel = "match:cancel";
        private const string EventRoundCommit = "round:commit";
        private const string EventRoundReveal = "round:reveal";
        private const string EventMatchSurrender = "match:surrender";

        private const string EventMatchFound = "match:found";
        private const string EventMatchError = "match:error";
        private const string EventRoundStart = "round:start";
        private const string EventBothCommitted = "round:both-committed";
        private const string EventRoundResults = "round:results";
        private const string EventMatchEnd = "match:end";

        #endregion

        #region Fields

        private readonly SocketIOClient _socket;
        private bool _disposed;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the server confirms a match was found.
        /// Subscribers receive the deserialized MatchFoundMessage.
        /// </summary>
        public event Action<MatchFoundMessage> OnMatchFound;

        /// <summary>
        /// Fired when both players have committed their actions.
        /// Client should now reveal.
        /// </summary>
        public event Action OnBothCommitted;

        /// <summary>
        /// Fired when the server sends round resolution results.
        /// </summary>
        public event Action<RoundResultsMessage> OnRoundResults;

        /// <summary>
        /// Fired when the full match ends.
        /// </summary>
        public event Action<MatchEndMessage> OnMatchEnded;

        /// <summary>
        /// Fired when the server starts a new round's planning phase.
        /// </summary>
        public event Action<RoundStartMessage> OnRoundStart;

        /// <summary>
        /// Fired on server-side match/matchmaking errors.
        /// </summary>
        public event Action<string> OnMatchError;

        #endregion

        public MatchNetworkController(SocketIOClient socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            SubscribeServerEvents();
            SubscribeConnectionEvents();
        }

        #region Client → Server

        /// <summary>
        /// Requests matchmaking with the selected hero.
        /// </summary>
        public void FindMatch(string heroId)
        {
            var msg = new FindMatchMessage { HeroId = heroId };
            _socket.Emit(EventMatchFind, JsonUtility.ToJson(msg));
        }

        /// <summary>
        /// Cancels ongoing matchmaking.
        /// </summary>
        public void CancelMatchmaking()
        {
            _socket.Emit(EventMatchCancel);
        }

        /// <summary>
        /// Sends the SHA-256 commitment hash.
        /// </summary>
        public void CommitActions(string hash)
        {
            var msg = new CommitMessage { Hash = hash };
            _socket.Emit(EventRoundCommit, JsonUtility.ToJson(msg));
        }

        /// <summary>
        /// Reveals the actual actions + nonce for server verification.
        /// </summary>
        public void RevealActions(ActionType[] actions, string nonce)
        {
            var msg = new RevealMessage { Actions = actions, Nonce = nonce };
            _socket.Emit(EventRoundReveal, JsonUtility.ToJson(msg));
        }

        /// <summary>
        /// Forfeits the current match.
        /// </summary>
        public void Surrender()
        {
            _socket.Emit(EventMatchSurrender);
        }

        #endregion

        #region Server → Client (handlers)

        private void HandleMatchFound(string json)
        {
            var msg = JsonUtility.FromJson<MatchFoundMessage>(json);
            Debug.Log($"[MatchNet] Match found: {msg.MatchId}");
            OnMatchFound?.Invoke(msg);
        }

        private void HandleMatchError(string json)
        {
            Debug.LogWarning($"[MatchNet] Server error: {json}");
            OnMatchError?.Invoke(json);
            GameEvents.NetworkError(json);
        }

        private void HandleRoundStart(string json)
        {
            var msg = JsonUtility.FromJson<RoundStartMessage>(json);
            Debug.Log($"[MatchNet] Round {msg.RoundNumber} starting (time: {msg.PlanningTime}s)");
            OnRoundStart?.Invoke(msg);
            GameEvents.RoundStarted(msg.RoundNumber);
            GameEvents.PhaseChanged(GamePhase.Planning);
        }

        private void HandleBothCommitted(string _)
        {
            Debug.Log("[MatchNet] Both committed — reveal now");
            OnBothCommitted?.Invoke();
        }

        private void HandleRoundResults(string json)
        {
            var msg = JsonUtility.FromJson<RoundResultsMessage>(json);
            Debug.Log($"[MatchNet] Round results received ({msg.Steps?.Length ?? 0} steps)");
            OnRoundResults?.Invoke(msg);
            GameEvents.PhaseChanged(GamePhase.Execution);
        }

        private void HandleMatchEnd(string json)
        {
            var msg = JsonUtility.FromJson<MatchEndMessage>(json);
            Debug.Log($"[MatchNet] Match ended: {msg.Result}");
            OnMatchEnded?.Invoke(msg);
            GameEvents.MatchEnded(msg.Result);
        }

        #endregion

        #region Connection Lifecycle

        private void HandleConnected()
        {
            GameEvents.NetworkConnected(_socket.SessionId);
        }

        private void HandleDisconnected(string reason)
        {
            GameEvents.NetworkDisconnected(reason);
        }

        private void HandleError(string error)
        {
            GameEvents.NetworkError(error);
        }

        #endregion

        #region Setup / Teardown

        private void SubscribeServerEvents()
        {
            _socket.On(EventMatchFound, HandleMatchFound);
            _socket.On(EventMatchError, HandleMatchError);
            _socket.On(EventRoundStart, HandleRoundStart);
            _socket.On(EventBothCommitted, HandleBothCommitted);
            _socket.On(EventRoundResults, HandleRoundResults);
            _socket.On(EventMatchEnd, HandleMatchEnd);
        }

        private void SubscribeConnectionEvents()
        {
            _socket.OnConnected += HandleConnected;
            _socket.OnDisconnected += HandleDisconnected;
            _socket.OnError += HandleError;
        }

        private void UnsubscribeAll()
        {
            _socket.OffAll(EventMatchFound);
            _socket.OffAll(EventMatchError);
            _socket.OffAll(EventRoundStart);
            _socket.OffAll(EventBothCommitted);
            _socket.OffAll(EventRoundResults);
            _socket.OffAll(EventMatchEnd);

            _socket.OnConnected -= HandleConnected;
            _socket.OnDisconnected -= HandleDisconnected;
            _socket.OnError -= HandleError;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnsubscribeAll();
        }

        #endregion
    }
}
