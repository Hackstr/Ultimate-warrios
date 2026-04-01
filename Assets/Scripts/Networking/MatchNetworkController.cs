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

        private const string EventMatchRejoin = "match:rejoin";

        private const string EventMatchFound = "match:found";
        private const string EventMatchError = "match:error";
        private const string EventRoundStart = "round:start";
        private const string EventBothCommitted = "round:both-committed";
        private const string EventRoundResults = "round:results";
        private const string EventMatchEnd = "match:end";
        private const string EventRejoinAck = "match:rejoin:ack";
        private const string EventOpponentDisconnected = "match:opponent-disconnected";
        private const string EventOpponentReconnected = "match:opponent-reconnected";

        #endregion

        #region Fields

        private readonly SocketIOClient _socket;
        private bool _disposed;

        #endregion

        #region Events

        public event Action<MatchFoundMessage> OnMatchFound;
        public event Action OnBothCommitted;
        public event Action<RoundResultsMessage> OnRoundResults;
        public event Action<MatchEndMessage> OnMatchEnded;
        public event Action<RoundStartMessage> OnRoundStart;
        public event Action<string> OnMatchError;
        public event Action<RejoinAckMessage> OnRejoinAck;
        public event Action<OpponentDisconnectedMessage> OnOpponentDisconnected;
        public event Action OnOpponentReconnected;

        #endregion

        public MatchNetworkController(SocketIOClient socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            SubscribeServerEvents();
            SubscribeConnectionEvents();
        }

        #region Client → Server

        public void FindMatch(string heroId, int rankTier = 0)
        {
            var msg = new FindMatchMessage { heroId = heroId, rankTier = rankTier };
            _socket.Emit(EventMatchFind, JsonUtility.ToJson(msg));
        }

        public void CancelMatchmaking()
        {
            _socket.Emit(EventMatchCancel);
        }

        public void CommitActions(string hash)
        {
            var msg = new CommitMessage { hash = hash };
            _socket.Emit(EventRoundCommit, JsonUtility.ToJson(msg));
        }

        public void RevealActions(ActionType[] actions, string nonce)
        {
            int[] intActions = new int[actions.Length];
            for (int i = 0; i < actions.Length; i++)
                intActions[i] = (int)actions[i];

            var msg = new RevealMessage { actions = intActions, nonce = nonce };
            _socket.Emit(EventRoundReveal, JsonUtility.ToJson(msg));
        }

        public void Surrender()
        {
            _socket.Emit(EventMatchSurrender);
        }

        public void Rejoin()
        {
            _socket.Emit(EventMatchRejoin);
        }

        #endregion

        #region Server → Client (handlers)

        private void HandleMatchFound(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<MatchFoundMessage>(json);
                Debug.Log($"[MatchNet] Match found: {msg.matchId}");
                OnMatchFound?.Invoke(msg);
            }
            catch (System.Exception ex) { Debug.LogError($"[MatchNet] Parse error (match:found): {ex.Message}"); }
        }

        private void HandleMatchError(string json)
        {
            Debug.LogWarning($"[MatchNet] Server error: {json}");
            OnMatchError?.Invoke(json);
            GameEvents.NetworkError(json);
        }

        private void HandleException(string json)
        {
            Debug.LogError($"[MatchNet] WS Exception: {json}");
            OnMatchError?.Invoke($"Server exception: {json}");
        }

        private void HandleRoundStart(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<RoundStartMessage>(json);
                Debug.Log($"[MatchNet] Round {msg.roundNumber} starting (time: {msg.timeLimit}s)");
                OnRoundStart?.Invoke(msg);
                GameEvents.RoundStarted(msg.roundNumber);
                GameEvents.PhaseChanged(GamePhase.Planning);
            }
            catch (System.Exception ex) { Debug.LogError($"[MatchNet] Parse error (round:start): {ex.Message}"); }
        }

        private void HandleBothCommitted(string _)
        {
            Debug.Log("[MatchNet] Both committed — reveal now");
            OnBothCommitted?.Invoke();
        }

        private void HandleRoundResults(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<RoundResultsMessage>(json);
                Debug.Log($"[MatchNet] Round results received ({msg.steps?.Length ?? 0} steps)");
                OnRoundResults?.Invoke(msg);
                GameEvents.PhaseChanged(GamePhase.Execution);
            }
            catch (System.Exception ex) { Debug.LogError($"[MatchNet] Parse error (round:results): {ex.Message}"); }
        }

        private void HandleMatchEnd(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<MatchEndMessage>(json);
                Debug.Log($"[MatchNet] Match ended: {(MatchResult)msg.winner}");
                OnMatchEnded?.Invoke(msg);
                GameEvents.MatchEnded((MatchResult)msg.winner);
            }
            catch (System.Exception ex) { Debug.LogError($"[MatchNet] Parse error (match:end): {ex.Message}"); }
        }

        private void HandleRejoinAck(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<RejoinAckMessage>(json);
                Debug.Log($"[MatchNet] Rejoin ack: success={msg.success}");
                OnRejoinAck?.Invoke(msg);
            }
            catch (System.Exception ex) { Debug.LogError($"[MatchNet] Parse error (rejoin:ack): {ex.Message}"); }
        }

        private void HandleOpponentDisconnected(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<OpponentDisconnectedMessage>(json);
                Debug.Log($"[MatchNet] Opponent disconnected (grace: {msg.gracePeriod}s)");
                OnOpponentDisconnected?.Invoke(msg);
            }
            catch (System.Exception ex) { Debug.LogError($"[MatchNet] Parse error (opponent-disconnected): {ex.Message}"); }
        }

        private void HandleOpponentReconnected(string _)
        {
            Debug.Log("[MatchNet] Opponent reconnected");
            OnOpponentReconnected?.Invoke();
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
            _socket.On(EventRejoinAck, HandleRejoinAck);
            _socket.On(EventOpponentDisconnected, HandleOpponentDisconnected);
            _socket.On(EventOpponentReconnected, HandleOpponentReconnected);
            _socket.On("exception", HandleException);
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
            _socket.OffAll(EventRejoinAck);
            _socket.OffAll(EventOpponentDisconnected);
            _socket.OffAll(EventOpponentReconnected);
            _socket.OffAll("exception");

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
