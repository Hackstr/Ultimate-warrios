#if UNITY_EDITOR
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TacticalDuelist.Platform.Editor
{
    /// <summary>
    /// Real Engine.IO v4 + Socket.IO v4 transport for Unity Editor testing.
    /// Uses System.Net.WebSockets.ClientWebSocket.
    /// Dispatches all events on Unity's main thread via UniTask.
    /// </summary>
    internal sealed class EditorSocketIOTransport : IWebSocketTransport
    {
        #region Fields

        private readonly string _serverUrl;
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private string _authToken;
        private string _engineSid;
        private bool _socketIOConnected;

        #endregion

        #region Events

        public event Action<string, string> OnMessage;
        public event Action<string> OnError;
        public event Action OnDisconnected;

        #endregion

        public EditorSocketIOTransport(string serverUrl)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        }

        #region IWebSocketTransport

        public void SetAuthToken(string token) => _authToken = token;

        public async UniTask Connect()
        {
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            _socketIOConnected = false;

            var wsUrl = BuildWsUrl(_serverUrl);
            Debug.Log($"[EditorWS] Connecting to {wsUrl}");

            try
            {
                await _ws.ConnectAsync(new Uri(wsUrl), _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorWS] Connection failed: {ex.Message}");
                throw;
            }

            Debug.Log("[EditorWS] WebSocket connected, awaiting Engine.IO handshake...");
            ReceiveLoopAsync(_cts.Token).Forget();
        }

        public void Send(string eventName, string jsonPayload)
        {
            if (_ws?.State != WebSocketState.Open || !_socketIOConnected) return;

            var packet = $"42[\"{EscapeJson(eventName)}\",{jsonPayload}]";
            SendRawAsync(packet).Forget();
        }

        public void Disconnect()
        {
            _socketIOConnected = false;
            _cts?.Cancel();

            if (_ws?.State == WebSocketState.Open)
            {
                try
                {
                    _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
                       .ContinueWith(_ => { });
                }
                catch { /* best-effort close */ }
            }

            _ws?.Dispose();
            _ws = null;
        }

        #endregion

        #region Engine.IO URL

        private static string BuildWsUrl(string serverUrl)
        {
            var url = serverUrl
                .Replace("https://", "wss://")
                .Replace("http://", "ws://");

            if (!url.StartsWith("ws", StringComparison.OrdinalIgnoreCase))
                url = "ws://" + url;

            url = url.TrimEnd('/');
            return $"{url}/socket.io/?EIO=4&transport=websocket";
        }

        #endregion

        #region Send

        private async UniTask SendRawAsync(string data)
        {
            if (_ws?.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(data);
            try
            {
                await _ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorWS] Send error: {ex.Message}");
            }
        }

        #endregion

        #region Receive Loop

        private async UniTaskVoid ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[16384];
            var sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    }
                    catch (OperationCanceledException) { return; }
                    catch (WebSocketException ex)
                    {
                        await UniTask.SwitchToMainThread();
                        OnError?.Invoke(ex.Message);
                        OnDisconnected?.Invoke();
                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await UniTask.SwitchToMainThread();
                        OnDisconnected?.Invoke();
                        return;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (!result.EndOfMessage) continue;

                    var raw = sb.ToString();
                    sb.Clear();

                    await UniTask.SwitchToMainThread();
                    ProcessEngineIOPacket(raw);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorWS] ReceiveLoop fatal: {ex}");
                try
                {
                    await UniTask.SwitchToMainThread();
                    OnError?.Invoke(ex.Message);
                    OnDisconnected?.Invoke();
                }
                catch { /* suppress errors during teardown */ }
            }
        }

        #endregion

        #region Packet Processing

        private void ProcessEngineIOPacket(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;

            char eioType = raw[0];
            string payload = raw.Length > 1 ? raw.Substring(1) : "";

            switch (eioType)
            {
                case '0': // Engine.IO OPEN
                    HandleEngineOpen(payload);
                    break;

                case '2': // Engine.IO PING → respond with PONG
                    SendRawAsync("3").Forget();
                    break;

                case '3': // Engine.IO PONG
                    break;

                case '4': // Engine.IO MESSAGE → delegate to Socket.IO
                    ProcessSocketIOPacket(payload);
                    break;

                case '6': // Engine.IO NOOP
                    break;

                default:
                    Debug.LogWarning($"[EditorWS] Unknown EIO type: {eioType} data={raw}");
                    break;
            }
        }

        private void HandleEngineOpen(string json)
        {
            Debug.Log($"[EditorWS] Engine.IO OPEN: {json}");
            _engineSid = ExtractJsonString(json, "sid");

            string connectPacket = string.IsNullOrEmpty(_authToken)
                ? "40"
                : $"40{{\"token\":\"{EscapeJson(_authToken)}\"}}";

            Debug.Log($"[EditorWS] Sending Socket.IO CONNECT (auth={!string.IsNullOrEmpty(_authToken)})");
            SendRawAsync(connectPacket).Forget();
        }

        private void ProcessSocketIOPacket(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            char sioType = data[0];
            string sioPayload = data.Length > 1 ? data.Substring(1) : "";

            switch (sioType)
            {
                case '0': // Socket.IO CONNECT ACK
                    _socketIOConnected = true;
                    Debug.Log($"[EditorWS] Socket.IO connected (sid={ExtractJsonString(sioPayload, "sid")})");
                    break;

                case '1': // Socket.IO DISCONNECT
                    _socketIOConnected = false;
                    Debug.Log("[EditorWS] Socket.IO server disconnected");
                    OnDisconnected?.Invoke();
                    break;

                case '2': // Socket.IO EVENT
                    ParseAndDispatchEvent(sioPayload);
                    break;

                case '4': // Socket.IO CONNECT_ERROR
                    Debug.LogError($"[EditorWS] Socket.IO connect error: {sioPayload}");
                    OnError?.Invoke($"connect_error: {sioPayload}");
                    break;

                default:
                    break;
            }
        }

        private void ParseAndDispatchEvent(string data)
        {
            if (string.IsNullOrEmpty(data) || data[0] != '[') return;

            int firstQuote = data.IndexOf('"');
            if (firstQuote < 0) return;
            int secondQuote = data.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0) return;

            string eventName = data.Substring(firstQuote + 1, secondQuote - firstQuote - 1);

            string jsonPayload = "{}";
            int commaIndex = data.IndexOf(',', secondQuote);
            if (commaIndex >= 0)
            {
                int lastBracket = data.LastIndexOf(']');
                if (lastBracket > commaIndex)
                    jsonPayload = data.Substring(commaIndex + 1, lastBracket - commaIndex - 1).Trim();
            }

            OnMessage?.Invoke(eventName, jsonPayload);
        }

        #endregion

        #region JSON Helpers

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var pattern = $"\"{key}\":\"";
            int start = json.IndexOf(pattern, StringComparison.Ordinal);
            if (start < 0) return null;
            start += pattern.Length;
            int end = json.IndexOf('"', start);
            return end > start ? json.Substring(start, end - start) : null;
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        #endregion
    }
}
#endif
