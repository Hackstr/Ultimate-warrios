using System;
using Cysharp.Threading.Tasks;

namespace TacticalDuelist.Platform
{
    /// <summary>
    /// Platform-agnostic WebSocket transport.
    /// WebGL uses jslib interop; native platforms use ClientWebSocket.
    /// Created via IPlatformNetwork.CreateWebSocket().
    /// </summary>
    public interface IWebSocketTransport
    {
        /// <summary>
        /// Sets the JWT auth token to include in the Socket.IO CONNECT handshake.
        /// Must be called before Connect().
        /// </summary>
        void SetAuthToken(string token);

        UniTask Connect();
        void Send(string eventName, string jsonPayload);
        void Disconnect();

        event Action<string, string> OnMessage;
        event Action<string> OnError;
        event Action OnDisconnected;
    }
}
