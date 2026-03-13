#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;

namespace TacticalDuelist.Platform.WebGL
{
    /// <summary>
    /// Provides configuration for the WebGL build.
    /// Server URL is determined from the page origin at runtime,
    /// or can be overridden via URL query parameter ?server=https://...
    /// </summary>
    public static class WebGLConfig
    {
        private static string _cachedUrl;

        /// <summary>
        /// Returns the server URL for API calls and Socket.IO.
        /// In production, derives from window.location.origin.
        /// Override by appending ?server=https://your.server.com to the URL.
        /// </summary>
        public static string GetServerUrl()
        {
            if (!string.IsNullOrEmpty(_cachedUrl))
                return _cachedUrl;

            _cachedUrl = GetServerUrlFromBrowser();
            Debug.Log($"[WebGLConfig] Server URL: {_cachedUrl}");
            return _cachedUrl;
        }

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string WebGL_GetServerUrl();

        private static string GetServerUrlFromBrowser()
        {
            try
            {
                return WebGL_GetServerUrl();
            }
            catch
            {
                return "https://api.tactical-duelist.com";
            }
        }
    }
}
#endif
