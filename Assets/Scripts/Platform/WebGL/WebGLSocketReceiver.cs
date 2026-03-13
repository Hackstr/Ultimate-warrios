#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;

namespace TacticalDuelist.Platform.WebGL
{
    /// <summary>
    /// MonoBehaviour that receives SendMessage callbacks from SocketIOPlugin.jslib.
    /// The JavaScript side calls Module.SendMessage('WebGLSocketReceiver', method, data).
    /// Must be attached to a GameObject named "WebGLSocketReceiver" in the scene.
    /// PlatformBootstrap creates this automatically.
    /// </summary>
    public sealed class WebGLSocketReceiver : MonoBehaviour
    {
        private static WebGLWebSocketTransport _transport;

        internal static void SetTransport(WebGLWebSocketTransport transport)
        {
            _transport = transport;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Called by JS when Socket.IO connects.</summary>
        public void OnSocketConnectedCallback(string _unused)
        {
            _transport?.HandleConnected();
        }

        /// <summary>Called by JS when Socket.IO disconnects.</summary>
        public void OnSocketDisconnectedCallback(string reason)
        {
            _transport?.HandleDisconnected(reason);
        }

        /// <summary>Called by JS on connection or transport error.</summary>
        public void OnSocketErrorReceived(string error)
        {
            _transport?.HandleError(error);
        }

        /// <summary>
        /// Called by JS for every Socket.IO event via onAny.
        /// Payload is JSON: {"e":"eventName","d":"jsonPayload"}
        /// </summary>
        public void OnSocketMessage(string messageJson)
        {
            if (_transport == null) return;

            var wrapper = JsonUtility.FromJson<SocketMessageWrapper>(messageJson);
            if (wrapper != null && !string.IsNullOrEmpty(wrapper.e))
            {
                _transport.HandleMessage(wrapper.e, wrapper.d ?? "{}");
            }
        }

        [System.Serializable]
        private class SocketMessageWrapper
        {
            public string e;
            public string d;
        }
    }
}
#endif
