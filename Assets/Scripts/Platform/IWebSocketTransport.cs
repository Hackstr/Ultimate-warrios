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
        UniTask Connect();
        void Send(string eventName, string jsonPayload);
        void Disconnect();

        event Action<string, string> OnMessage;
        event Action<string> OnError;
        event Action OnDisconnected;
    }
}
