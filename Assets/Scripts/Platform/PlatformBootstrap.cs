using UnityEngine;

namespace TacticalDuelist.Platform
{
    /// <summary>
    /// Entry point that selects the correct IPlatformService at startup.
    /// Attach to a GameObject in the boot scene. Runs before any game logic.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PlatformBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var platform = CreatePlatformService();
            platform.Initialize();

            ServiceLocator.Register<IPlatformService>(platform);
            ServiceLocator.Register(platform.Auth);
            ServiceLocator.Register(platform.Storage);
            ServiceLocator.Register(platform.Network);
            ServiceLocator.Register(platform.Haptics);
            ServiceLocator.Register(platform.Notifications);
            ServiceLocator.Register(platform.DeepLinks);
            ServiceLocator.Register(platform.Share);

            Debug.Log($"[PlatformBootstrap] Initialized: {platform.CurrentPlatform}");

#if UNITY_WEBGL && !UNITY_EDITOR
            EnsureWebGLReceiver();
            EnsureWebGLInputBridge();
#endif

            DontDestroyOnLoad(gameObject);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static void EnsureWebGLReceiver()
        {
            if (GameObject.Find("WebGLSocketReceiver") != null) return;
            var go = new GameObject("WebGLSocketReceiver");
            go.AddComponent<WebGL.WebGLSocketReceiver>();
        }

        private static void EnsureWebGLInputBridge()
        {
            if (GameObject.Find("WebGLInputBridge") != null) return;
            var go = new GameObject("WebGLInputBridge");
            go.AddComponent<WebGL.WebGLInputBridge>();
        }
#endif

        private static IPlatformService CreatePlatformService()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGL.WebGLPlatform();
#elif UNITY_ANDROID && !UNITY_EDITOR
            // TODO: return new Android.AndroidPlatform();
            Debug.LogWarning("[PlatformBootstrap] Android platform not yet implemented, falling back to Editor");
            return new Editor.EditorPlatform();
#elif UNITY_IOS && !UNITY_EDITOR
            // TODO: return new iOS.iOSPlatform();
            Debug.LogWarning("[PlatformBootstrap] iOS platform not yet implemented, falling back to Editor");
            return new Editor.EditorPlatform();
#else
            return new Editor.EditorPlatform();
#endif
        }
    }
}
