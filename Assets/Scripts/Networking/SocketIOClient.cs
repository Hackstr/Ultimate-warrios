using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TacticalDuelist.Platform;
using UnityEngine;

namespace TacticalDuelist.Networking
{
    /// <summary>
    /// Platform-agnostic Socket.IO–style client built on IWebSocketTransport.
    /// Handles connect/disconnect, event emit/subscribe, and auto-reconnect
    /// with exponential backoff.
    /// </summary>
    public sealed class SocketIOClient : IDisposable
    {
        #region Constants

        private const int MaxReconnectAttempts = 5;
        private const float InitialReconnectDelaySec = 1f;
        private const float MaxReconnectDelaySec = 30f;

        #endregion

        #region Fields

        private readonly string _serverUrl;
        private IWebSocketTransport _transport;
        private readonly Dictionary<string, List<Action<string>>> _listeners = new();
        private bool _intentionalDisconnect;
        private int _reconnectAttempt;
        private bool _connected;
        private bool _disposed;
        private string _authToken;

        #endregion

        #region Properties

        public bool IsConnected => _connected;
        public string SessionId { get; private set; }

        #endregion

        #region Events

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;

        #endregion

        public SocketIOClient(string serverUrl)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        }

        #region Public API

        /// <summary>
        /// Sets the JWT auth token for Socket.IO CONNECT handshake.
        /// Must be called before ConnectAsync().
        /// </summary>
        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        /// <summary>
        /// Establishes connection via platform's WebSocket transport.
        /// </summary>
        public async UniTask ConnectAsync()
        {
            _intentionalDisconnect = false;
            _reconnectAttempt = 0;
            await CreateAndConnect();
        }

        /// <summary>
        /// Emits a named event with JSON payload to the server.
        /// The transport handles protocol framing (e.g. 42["event",{...}]).
        /// </summary>
        public void Emit(string eventName, string jsonPayload = "{}")
        {
            if (!_connected)
            {
                Debug.LogWarning($"[SocketIO] Cannot emit '{eventName}': not connected");
                return;
            }
            _transport.Send(eventName, jsonPayload);
        }

        /// <summary>
        /// Subscribes to a named server event.
        /// </summary>
        public void On(string eventName, Action<string> handler)
        {
            if (!_listeners.TryGetValue(eventName, out var handlers))
            {
                handlers = new List<Action<string>>();
                _listeners[eventName] = handlers;
            }
            handlers.Add(handler);
        }

        /// <summary>
        /// Removes a specific handler for an event.
        /// </summary>
        public void Off(string eventName, Action<string> handler)
        {
            if (_listeners.TryGetValue(eventName, out var handlers))
                handlers.Remove(handler);
        }

        /// <summary>
        /// Removes all handlers for an event.
        /// </summary>
        public void OffAll(string eventName)
        {
            _listeners.Remove(eventName);
        }

        /// <summary>
        /// Intentional disconnect. Does not trigger reconnect.
        /// </summary>
        public void Disconnect()
        {
            _intentionalDisconnect = true;
            _connected = false;
            _transport?.Disconnect();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            _listeners.Clear();
        }

        #endregion

        #region Private

        private async UniTask CreateAndConnect()
        {
            var network = ServiceLocator.Get<IPlatformNetwork>();
            _transport = network.CreateWebSocket(_serverUrl);

            if (!string.IsNullOrEmpty(_authToken))
                _transport.SetAuthToken(_authToken);

            _transport.OnMessage += HandleMessage;
            _transport.OnError += HandleError;
            _transport.OnDisconnected += HandleDisconnect;

            await _transport.Connect();

            _connected = true;
            _reconnectAttempt = 0;
            SessionId = Guid.NewGuid().ToString("N")[..12];

            Debug.Log($"[SocketIO] Connected to {_serverUrl} (session: {SessionId})");
            OnConnected?.Invoke();
        }

        private void HandleMessage(string eventName, string jsonPayload)
        {
            if (!_listeners.TryGetValue(eventName, out var handlers)) return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                try
                {
                    handlers[i]?.Invoke(jsonPayload);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIO] Handler error for '{eventName}': {ex.Message}");
                }
            }
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[SocketIO] Transport error: {error}");
            OnError?.Invoke(error);
        }

        private void HandleDisconnect()
        {
            _connected = false;
            DetachTransportEvents();

            if (_intentionalDisconnect || _disposed)
            {
                OnDisconnected?.Invoke("client_disconnect");
                return;
            }

            Debug.LogWarning("[SocketIO] Unexpected disconnect, attempting reconnect...");
            OnDisconnected?.Invoke("transport_close");
            TryReconnect().Forget();
        }

        private async UniTaskVoid TryReconnect()
        {
            while (_reconnectAttempt < MaxReconnectAttempts && !_intentionalDisconnect && !_disposed)
            {
                _reconnectAttempt++;
                float delay = Mathf.Min(
                    InitialReconnectDelaySec * Mathf.Pow(2, _reconnectAttempt - 1),
                    MaxReconnectDelaySec);

                Debug.Log($"[SocketIO] Reconnect attempt {_reconnectAttempt}/{MaxReconnectAttempts} in {delay:F1}s");
                await UniTask.Delay(TimeSpan.FromSeconds(delay));

                if (_intentionalDisconnect || _disposed) return;

                try
                {
                    await CreateAndConnect();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIO] Reconnect failed: {ex.Message}");
                }
            }

            Debug.LogError("[SocketIO] All reconnect attempts exhausted");
            OnError?.Invoke("reconnect_failed");
        }

        private void DetachTransportEvents()
        {
            if (_transport == null) return;
            _transport.OnMessage -= HandleMessage;
            _transport.OnError -= HandleError;
            _transport.OnDisconnected -= HandleDisconnect;
        }

        #endregion
    }
}
