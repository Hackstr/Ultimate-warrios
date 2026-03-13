#if UNITY_WEBGL
using UnityEngine;

namespace TacticalDuelist.Platform.WebGL
{
    /// <summary>
    /// Placeholder — kept for SendMessage compatibility.
    /// Currently testing if native Input System handles clicks without bridge.
    /// </summary>
    public class WebGLInputBridge : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<WebGLInputBridge>() != null) return;
            var go = new GameObject("WebGLInputBridge");
            go.AddComponent<WebGLInputBridge>();
            DontDestroyOnLoad(go);
            Debug.Log("[WebGLInputBridge] Created (passive mode)");
        }

        public void OnPointerDown(string coords)
        {
            // Disabled — testing native Input System
            Debug.Log($"[WebGLInputBridge] Received coords: {coords} (bridge disabled, using native input)");
        }
    }
}
#endif
