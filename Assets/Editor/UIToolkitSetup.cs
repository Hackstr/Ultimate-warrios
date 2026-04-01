using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TacticalDuelist.Editor
{
    /// <summary>
    /// Sets up UI Toolkit infrastructure in the active scene.
    /// Creates UIDocument + UIManager + PanelSettings.
    /// Menu: Tactical Duelist > Setup UI Toolkit
    /// </summary>
    public static class UIToolkitSetup
    {
        private const string ScreensPath = "Assets/Scripts/UI/Toolkit/Screens";
        private const string PanelSettingsPath = "Assets/Scripts/UI/Toolkit/TacticalDuelistPanel.asset";

        [MenuItem("Tactical Duelist/Setup UI Toolkit")]
        public static void SetupUIToolkit()
        {
            // 1. Create or update PanelSettings
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                Debug.Log($"[UIToolkitSetup] Created PanelSettings at {PanelSettingsPath}");
            }
            // Reference resolution matches target: 1080x1920 mobile portrait
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);

            // 2. Find or create UIManager GameObject
            var uiManager = Object.FindAnyObjectByType<UI.Toolkit.UIManager>();
            if (uiManager == null)
            {
                var go = new GameObject("UIManager");
                var uiDoc = go.AddComponent<UIDocument>();
                uiDoc.panelSettings = panelSettings;
                uiManager = go.AddComponent<UI.Toolkit.UIManager>();
                Debug.Log("[UIToolkitSetup] Created UIManager GameObject");
            }

            // 3. Wire UIDocument
            var doc = uiManager.GetComponent<UIDocument>();
            if (doc != null && doc.panelSettings == null)
                doc.panelSettings = panelSettings;

            // 4. Wire VisualTreeAsset references via SerializedObject
            var so = new SerializedObject(uiManager);
            SetTemplate(so, "_uiDocument", doc);
            SetVTA(so, "_mainMenuTemplate", "MainMenuScreen");
            SetVTA(so, "_heroSelectTemplate", "HeroSelectScreen");
            SetVTA(so, "_matchmakingTemplate", "MatchmakingScreen");
            SetVTA(so, "_planningTemplate", "PlanningScreen");
            SetVTA(so, "_resultTemplate", "ResultScreen");
            SetVTA(so, "_hudTemplate", "HUDScreen");
            SetVTA(so, "_splashTemplate", "SplashScreen");
            SetVTA(so, "_tutorialTemplate", "TutorialScreen");
            SetVTA(so, "_settingsTemplate", "SettingsScreen");
            SetVTA(so, "_heroesCollectionTemplate", "HeroesCollectionScreen");
            SetVTA(so, "_leaderboardTemplate", "LeaderboardScreen");
            SetVTA(so, "_roundTransitionTemplate", "RoundTransitionOverlay");
            SetVTA(so, "_revealTemplate", "RevealScreen");
            SetVTA(so, "_profileTemplate", "ProfileScreen");
            SetVTA(so, "_preMatchTemplate", "PreMatchScreen");
            SetVTA(so, "_reconnectingTemplate", "ReconnectingScreen");
            SetVTA(so, "_toastTemplate", "ToastContainer");
            so.ApplyModifiedProperties();

            // 5. Wire GameBootstrap reference
            var bootstrap = Object.FindAnyObjectByType<Gameplay.GameBootstrap>();
            if (bootstrap != null)
            {
                var bso = new SerializedObject(bootstrap);
                var prop = bso.FindProperty("_uiManagerPrefab");
                // Not a prefab — direct scene reference not needed since FindAnyObjectByType is used
                bso.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(uiManager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            AssetDatabase.SaveAssets();
            Debug.Log("[UIToolkitSetup] UI Toolkit setup complete! Save the scene.");
        }

        private static void SetTemplate(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void SetVTA(SerializedObject so, string field, string uxmlName)
        {
            var prop = so.FindProperty(field);
            if (prop == null) return;

            var path = $"{ScreensPath}/{uxmlName}.uxml";
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (asset != null)
                prop.objectReferenceValue = asset;
            else
                Debug.LogWarning($"[UIToolkitSetup] UXML not found: {path}");
        }
    }
}
