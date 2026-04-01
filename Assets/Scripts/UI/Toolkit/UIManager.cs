using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    /// <summary>
    /// Central manager for all UI Toolkit screens.
    /// Owns a single UIDocument with all screens instantiated as children.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Screen Templates")]
        [SerializeField] private VisualTreeAsset _mainMenuTemplate;
        [SerializeField] private VisualTreeAsset _heroSelectTemplate;
        [SerializeField] private VisualTreeAsset _matchmakingTemplate;
        [SerializeField] private VisualTreeAsset _planningTemplate;
        [SerializeField] private VisualTreeAsset _resultTemplate;
        [SerializeField] private VisualTreeAsset _hudTemplate;
        [SerializeField] private VisualTreeAsset _splashTemplate;

        [Header("Meta Screen Templates")]
        [SerializeField] private VisualTreeAsset _settingsTemplate;
        [SerializeField] private VisualTreeAsset _heroesCollectionTemplate;
        [SerializeField] private VisualTreeAsset _leaderboardTemplate;

        [Header("Tutorial")]
        [SerializeField] private VisualTreeAsset _tutorialTemplate;

        [Header("Profile")]
        [SerializeField] private VisualTreeAsset _profileTemplate;

        [Header("Pre-Match")]
        [SerializeField] private VisualTreeAsset _preMatchTemplate;

        [Header("Overlay Templates")]
        [SerializeField] private VisualTreeAsset _roundTransitionTemplate;
        [SerializeField] private VisualTreeAsset _revealTemplate;
        [SerializeField] private VisualTreeAsset _reconnectingTemplate;
        [SerializeField] private VisualTreeAsset _toastTemplate;

        // Screen controllers
        public MainMenuController MainMenu { get; private set; }
        public HeroSelectController HeroSelect { get; private set; }
        public MatchmakingController Matchmaking { get; private set; }
        public PlanningController Planning { get; private set; }
        public ResultController Result { get; private set; }
        public HUDController HUD { get; private set; }
        public SplashController Splash { get; private set; }

        // Meta screen controllers
        public SettingsController Settings { get; private set; }
        public HeroesCollectionController HeroesCollection { get; private set; }
        public LeaderboardController Leaderboard { get; private set; }

        // Tutorial
        public TutorialController Tutorial { get; private set; }

        // Profile
        public ProfileController Profile { get; private set; }

        // Pre-Match
        public PreMatchController PreMatch { get; private set; }

        // Overlay controllers
        public RoundTransitionController RoundTransition { get; private set; }
        public RevealController Reveal { get; private set; }
        public ReconnectingController Reconnecting { get; private set; }
        public ToastManager Toasts { get; private set; }

        private VisualElement _root;
        private UIScreenBase _activeScreen;
        private readonly List<UIScreenBase> _allScreens = new();

        private void Awake()
        {
            _root = _uiDocument.rootVisualElement;
            _root.style.width = Length.Percent(100);
            _root.style.height = Length.Percent(100);

            // Instantiate and bind all screens
            MainMenu = CreateScreen<MainMenuController>(_mainMenuTemplate, "main-menu");
            HeroSelect = CreateScreen<HeroSelectController>(_heroSelectTemplate, "hero-select");
            Matchmaking = CreateScreen<MatchmakingController>(_matchmakingTemplate, "matchmaking");
            Planning = CreateScreen<PlanningController>(_planningTemplate, "planning");
            Result = CreateScreen<ResultController>(_resultTemplate, "result");
            HUD = CreateScreen<HUDController>(_hudTemplate, "hud");

            if (_splashTemplate != null)
                Splash = CreateScreen<SplashController>(_splashTemplate, "splash");

            // Tutorial
            Tutorial = CreateScreen<TutorialController>(_tutorialTemplate, "tutorial");

            // Profile
            Profile = CreateScreen<ProfileController>(_profileTemplate, "profile");

            // Pre-Match
            PreMatch = CreateScreen<PreMatchController>(_preMatchTemplate, "prematch");

            // Meta screens
            Settings = CreateScreen<SettingsController>(_settingsTemplate, "settings");
            HeroesCollection = CreateScreen<HeroesCollectionController>(_heroesCollectionTemplate, "heroes-collection");
            Leaderboard = CreateScreen<LeaderboardController>(_leaderboardTemplate, "leaderboard");

            if (_roundTransitionTemplate != null)
                RoundTransition = CreateScreen<RoundTransitionController>(_roundTransitionTemplate, "round-transition");

            if (_reconnectingTemplate != null)
                Reconnecting = CreateScreen<ReconnectingController>(_reconnectingTemplate, "reconnecting");

            Reveal = CreateScreen<RevealController>(_revealTemplate, "reveal");

            // Toast container (always present, on top)
            if (_toastTemplate != null)
            {
                var toastRoot = InstantiateTemplate(_toastTemplate, "toasts");
                Toasts = new ToastManager();
                Toasts.Bind(toastRoot);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _activeScreen?.Tick(dt);
            Toasts?.Tick(dt);
        }

        private void OnDestroy()
        {
            foreach (var screen in _allScreens)
                screen.Dispose();
        }

        /// <summary>
        /// Shows a screen, hiding the current active one.
        /// HUD is managed separately (shown/hidden independently).
        /// </summary>
        public void ShowScreen(UIScreenBase screen)
        {
            if (_activeScreen == screen) return;

            _activeScreen?.Hide();
            _activeScreen = screen;

            // Only call Show() if not already visible (custom Show overloads
            // may have already called base.Show())
            if (!screen.IsVisible)
                screen.Show();
        }

        /// <summary>
        /// Shows the HUD overlay (independent of active screen).
        /// </summary>
        public void ShowHUD() => HUD?.Show();

        /// <summary>
        /// Hides the HUD overlay.
        /// </summary>
        public void HideHUD() => HUD?.Hide();

        /// <summary>
        /// Hides all screens and HUD.
        /// </summary>
        public void HideAll()
        {
            foreach (var screen in _allScreens)
                screen.Hide();
            _activeScreen = null;
        }

        private T CreateScreen<T>(VisualTreeAsset template, string name) where T : UIScreenBase, new()
        {
            if (template == null)
            {
                Debug.LogWarning($"[UIManager] Template for '{name}' is null, skipping.");
                return null;
            }

            var screenRoot = InstantiateTemplate(template, name);
            // Wrapper class handles show/hide; UXML child keeps .screen for styling
            screenRoot.AddToClassList("screen-wrapper");

            var controller = new T();
            controller.Bind(screenRoot);
            _allScreens.Add(controller);

            return controller;
        }

        private VisualElement InstantiateTemplate(VisualTreeAsset template, string name)
        {
            var container = template.Instantiate();
            container.name = name;
            container.style.position = Position.Absolute;
            container.style.width = Length.Percent(100);
            container.style.height = Length.Percent(100);
            _root.Add(container);
            return container;
        }
    }
}
