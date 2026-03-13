using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TacticalDuelist.Editor
{
    /// <summary>
    /// Menu items and utilities for configuring WebGL builds targeting Telegram Mini Apps.
    /// </summary>
    public static class WebGLBuildHelper
    {
        [MenuItem("Tactical Duelist/Configure WebGL Build Settings")]
        public static void ConfigureWebGLSettings()
        {
            PlayerSettings.WebGL.template = "PROJECT:TelegramMiniApp";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            PlayerSettings.companyName = "TacticalDuelist";
            PlayerSettings.productName = "Tactical Duelist";

            PlayerSettings.defaultScreenWidth = 960;
            PlayerSettings.defaultScreenHeight = 600;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowFullscreenSwitch = false;

            // .NET Standard 2.1 — fixes "script class layout is incompatible" errors
            // caused by pointer/Nullable fields in Unity internal packages (Collections,
            // InputSystem, RenderPipelines) when using .NET Framework profile with IL2CPP.
            PlayerSettings.SetApiCompatibilityLevel(
                BuildTargetGroup.WebGL,
                ApiCompatibilityLevel.NET_Standard
            );

            var webgl = NamedBuildTarget.WebGL;
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCodeGeneration(webgl, Il2CppCodeGeneration.OptimizeSize);

            Debug.Log("[WebGLBuildHelper] WebGL build settings configured for Telegram Mini App (.NET Standard 2.1)");
        }

        [MenuItem("Tactical Duelist/Clean IL2CPP Cache")]
        public static void CleanIL2CPPCache()
        {
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;

            string[] cacheDirs =
            {
                System.IO.Path.Combine(projectRoot, "Library", "Il2cppBuildCache"),
                System.IO.Path.Combine(projectRoot, "Library", "Bee"),
                System.IO.Path.Combine(projectRoot, "Library", "BuildPlayerData"),
            };

            foreach (var dir in cacheDirs)
            {
                if (System.IO.Directory.Exists(dir))
                {
                    Debug.Log($"[WebGLBuildHelper] Deleting cache: {dir}");
                    System.IO.Directory.Delete(dir, true);
                }
            }

            Debug.Log("[WebGLBuildHelper] IL2CPP/Bee caches cleared. Ready for clean build.");
        }

        [MenuItem("Tactical Duelist/Build WebGL (Clean)")]
        public static void BuildWebGLClean()
        {
            CleanIL2CPPCache();
            BuildWebGL();
        }

        [MenuItem("Tactical Duelist/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureWebGLSettings();

            string buildPath = System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath).FullName,
                "WebGLBuild"
            );

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuildHelper] Build succeeded: {buildPath}");
                EditorUtility.RevealInFinder(buildPath);
            }
            else
            {
                Debug.LogError($"[WebGLBuildHelper] Build failed: {report.summary.result}");
            }
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var result = new System.Collections.Generic.List<string>();
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                    result.Add(scene.path);
            }

            if (result.Count == 0)
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(activeScene.path))
                    result.Add(activeScene.path);
                else
                    Debug.LogWarning("[WebGLBuildHelper] No scenes in Build Settings and no saved active scene.");
            }

            return result.ToArray();
        }
    }
}
