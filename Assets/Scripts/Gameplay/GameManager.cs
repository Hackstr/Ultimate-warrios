using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;
using TacticalDuelist.Core.Utils;
using TacticalDuelist.Networking;
using TacticalDuelist.Platform;
using TacticalDuelist.UI.Toolkit;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Main scene entry point. Wires MatchManager (pure logic), UI screens,
    /// ExecutionController (visual playback), GridView, and optional networking.
    /// Manages the full game flow state machine independently of MatchManager's GamePhase.
    ///
    /// Now uses UIManager (UI Toolkit) instead of individual UGUI MonoBehaviour screens.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Flow State

        public enum FlowState
        {
            Tutorial,
            MainMenu,
            Matchmaking,
            HeroSelect,
            PreMatch,
            PlanningP1,
            PlanningP2,
            PlanningOnline,
            WaitingForOpponent,
            Reveal,
            Execution,
            PostRound,
            MatchResult,
            Reconnecting
        }

        #endregion

        #region Serialized Fields

        [Header("UI")]
        [SerializeField] private UIManager _ui;

        [Header("Gameplay")]
        [SerializeField] private ExecutionController _executionController;
        [SerializeField] private GridView _gridView;
        [SerializeField] private CameraController _cameraController;

        [Header("Maps")]
        [SerializeField] private MapConfig _defaultMap;
        [SerializeField] private MapConfig[] _allMaps;

        [Header("Network")]
        #pragma warning disable CS0414
        [SerializeField] private string _serverUrl = "http://localhost:3000";
        #pragma warning restore CS0414

        [Header("Mode")]
        [SerializeField] private bool _offlineMode = true;

        #endregion

        #region Fields

        private FlowState _currentState;
        private MatchManager _matchManager;
        private MatchNetworkController _networkController;
        private SocketIOClient _socket;
        private bool _vsBot;
        private bool _isTutorial;

        private HeroConfig _p1Hero;
        private HeroConfig _p2Hero;
        private MapConfig _activeMap;
        private HeroPreview3D _heroPreview;
        private HeroPreview3D _heroSelectPreview;

        private List<ActionType> _p1Actions;
        private List<ActionType> _lastP2Actions;
        private string _commitHash;
        private string _commitNonce;

        private readonly List<RoundResult> _roundResults = new();
        private int _onlineWinner = -1;
        private int _onlineRoundNumber = 1;
        private float _onlineTimeLimit = 30f;

        private RoundStartMessage _pendingRoundStart;
        private MatchEndMessage _pendingMatchEnd;

        // Reconnection state
        private string _currentMatchId;
        private FlowState _preReconnectState;

        // Post-match rewards (online)
        private int _lastRatingDelta;
        private int _lastCoinsEarned;

        // Waiting timeout
        private float _waitingTimer;
        private const float WaitingTimeoutSec = 90f;

        // Rejoin retry
        private int _rejoinAttempts;
        private float _rejoinTimer;
        private const int MaxRejoinAttempts = 3;
        private const float RejoinTimeoutSec = 5f;
        private bool _awaitingRejoinAck;

        #endregion

        #region Properties

        public FlowState CurrentState => _currentState;
        public bool IsOffline => _offlineMode;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_ui == null)
            {
                Debug.LogError("[GameManager] UIManager not wired! Cannot start.");
                return;
            }
            _ui.HideAll();

            // Wire voice line toast
            GameEvents.ShowToast = (text) => _ui.Toasts?.ShowToast(text, ToastType.Info);

            TransitionTo(FlowState.MainMenu);
        }

        private void Update()
        {
            if (_currentState == FlowState.WaitingForOpponent && !_offlineMode)
            {
                _waitingTimer += Time.deltaTime;
                if (_waitingTimer >= WaitingTimeoutSec)
                {
                    Debug.LogWarning("[GameManager] WaitingForOpponent timed out");
                    _ui.Toasts?.ShowToast("Opponent timed out", ToastType.Warning);
                    CleanupMatch();
                    TransitionTo(FlowState.MainMenu);
                }
            }

            // Rejoin ack timeout — retry if no response
            if (_currentState == FlowState.Reconnecting && _awaitingRejoinAck)
            {
                _rejoinTimer += Time.deltaTime;
                if (_rejoinTimer >= RejoinTimeoutSec)
                {
                    if (_rejoinAttempts < MaxRejoinAttempts)
                    {
                        Debug.LogWarning($"[GameManager] Rejoin ack timeout — retry {_rejoinAttempts + 1}/{MaxRejoinAttempts}");
                        SendRejoin();
                    }
                    else
                    {
                        Debug.LogError("[GameManager] All rejoin attempts failed");
                        _awaitingRejoinAck = false;
                        _ui.Reconnecting?.SetFailed();
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (_executionController != null)
                _executionController.OnPlaybackComplete += HandlePlaybackComplete;

            GameEvents.OnRoundEnded += CaptureRoundResult;
        }

        private void OnDisable()
        {
            if (_executionController != null)
                _executionController.OnPlaybackComplete -= HandlePlaybackComplete;

            GameEvents.OnRoundEnded -= CaptureRoundResult;

            UnsubscribeUI();
            _networkController?.Dispose();
        }

        private void OnDestroy()
        {
            _networkController?.Dispose();
            _socket?.Dispose();
            GameEvents.ClearAll();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called by GameBootstrap to wire runtime references.
        /// </summary>
        public void SetupRuntime(
            UIManager ui,
            ExecutionController execution,
            GridView gridView,
            CameraController cameraController,
            MapConfig defaultMap,
            MapConfig[] allMaps = null)
        {
            _ui = ui;
            _executionController = execution;
            _gridView = gridView;
            _cameraController = cameraController;
            _defaultMap = defaultMap;
            _allMaps = allMaps;
            _offlineMode = true;

            if (_executionController != null)
            {
                _executionController.OnPlaybackComplete -= HandlePlaybackComplete;
                _executionController.OnPlaybackComplete += HandlePlaybackComplete;
            }
        }

        public void SetHeroPreview(HeroPreview3D preview) => _heroPreview = preview;
        public void SetHeroSelectPreview(HeroPreview3D preview) => _heroSelectPreview = preview;

        public void StartOfflineGame()
        {
            _offlineMode = true;
            _vsBot = false;
            _networkController?.Dispose();
            _networkController = null;
            TransitionTo(FlowState.HeroSelect);
        }

        public void StartBotGame()
        {
            _offlineMode = true;
            _vsBot = true;
            _networkController?.Dispose();
            _networkController = null;
            TransitionTo(FlowState.HeroSelect);
        }

        public void StartOnlineGame(SocketIOClient socket)
        {
            _offlineMode = false;
            _networkController?.Dispose();
            _networkController = new MatchNetworkController(socket);
            SubscribeNetwork();
            TransitionTo(FlowState.HeroSelect);
        }

        public void ReturnToMainMenu()
        {
            CleanupMatch();
            TransitionTo(FlowState.MainMenu);
        }

        #endregion

        #region State Machine

        private void TransitionTo(FlowState newState)
        {
            Debug.Log($"[GameManager] {_currentState} -> {newState}");
            _currentState = newState;

            switch (newState)
            {
                case FlowState.Tutorial:        EnterTutorial(); break;
                case FlowState.MainMenu:        EnterMainMenu(); break;
                case FlowState.Matchmaking:     EnterMatchmaking(); break;
                case FlowState.HeroSelect:      EnterHeroSelect(); break;
                case FlowState.PreMatch:        EnterPreMatch(); break;
                case FlowState.PlanningP1:      EnterPlanningP1(); break;
                case FlowState.PlanningP2:      EnterPlanningP2(); break;
                case FlowState.PlanningOnline:  EnterPlanningOnline(); break;
                case FlowState.WaitingForOpponent: EnterWaitingForOpponent(); break;
                case FlowState.Reveal:          EnterReveal(); break;
                case FlowState.Execution:       EnterExecution(); break;
                case FlowState.PostRound:       EnterPostRound(); break;
                case FlowState.MatchResult:     EnterMatchResult(); break;
                case FlowState.Reconnecting:   EnterReconnecting(); break;
            }
        }

        #endregion

        #region State Entries

        private void EnterTutorial()
        {
            _ui.HideAll();
            _ui.HideHUD();

            if (_ui.Tutorial != null)
            {
                _ui.Tutorial.OnComplete -= HandleTutorialComplete;
                _ui.Tutorial.OnComplete += HandleTutorialComplete;
                _ui.Tutorial.OnSkip -= HandleTutorialSkip;
                _ui.Tutorial.OnSkip += HandleTutorialSkip;
                _ui.ShowScreen(_ui.Tutorial);
            }
            else
            {
                TransitionTo(FlowState.MainMenu);
            }
        }

        private void HandleTutorialComplete()
        {
            UnsubscribeTutorial();
            StartTutorialMatch();
        }

        private void HandleTutorialSkip()
        {
            UnsubscribeTutorial();
            PlayerPrefs.SetInt("tutorial_done", 1);
            PlayerPrefs.Save();
            TransitionTo(FlowState.MainMenu);
        }

        private void UnsubscribeTutorial()
        {
            if (_ui.Tutorial == null) return;
            _ui.Tutorial.OnComplete -= HandleTutorialComplete;
            _ui.Tutorial.OnSkip -= HandleTutorialSkip;
        }

        private void StartTutorialMatch()
        {
            _isTutorial = true;
            _offlineMode = true;
            _vsBot = true;

            // Pick Archer for player, Tank for tutorial bot
            var heroes = _ui.HeroSelect?.GetHeroes();
            _p1Hero = heroes?.Find(h => h.heroId == "archer") ?? heroes?[0];
            _p2Hero = heroes?.Find(h => h.heroId == "tank") ?? (heroes?.Count > 1 ? heroes[1] : _p1Hero);

            InitOfflineMatch();
            TransitionTo(FlowState.PlanningP1);
        }

        private void EnterMainMenu()
        {
            _ui.HideAll();
            _ui.HideHUD();
            _heroPreview?.SetVisible(true); // Show MainMenu 3D preview

            // Hide game heroes when returning to menu
            if (_executionController != null)
            {
                _executionController.Hero1View?.gameObject.SetActive(false);
                _executionController.Hero2View?.gameObject.SetActive(false);
            }

            if (_ui.MainMenu != null)
            {
                _ui.ShowScreen(_ui.MainMenu);
                _ui.MainMenu.OnPlayOffline -= StartOfflineGame;
                _ui.MainMenu.OnPlayOffline += StartOfflineGame;
                _ui.MainMenu.OnPlayOnline -= HandlePlayOnline;
                _ui.MainMenu.OnPlayOnline += HandlePlayOnline;
                _ui.MainMenu.OnPlayBot -= StartBotGame;
                _ui.MainMenu.OnPlayBot += StartBotGame;

                // Nav bar
                _ui.MainMenu.OnNavHeroes -= ShowHeroesCollection;
                _ui.MainMenu.OnNavHeroes += ShowHeroesCollection;
                _ui.MainMenu.OnNavRank -= ShowLeaderboard;
                _ui.MainMenu.OnNavRank += ShowLeaderboard;
                _ui.MainMenu.OnNavSettings -= ShowSettings;
                _ui.MainMenu.OnNavSettings += ShowSettings;

                // Player name tap → Profile
                _ui.MainMenu.OnNavProfile -= ShowProfile;
                _ui.MainMenu.OnNavProfile += ShowProfile;

                // Wallet
                _ui.MainMenu.OnConnectWallet -= HandleConnectWallet;
                _ui.MainMenu.OnConnectWallet += HandleConnectWallet;
                UpdateWalletUI();
            }

            // Wire back buttons for meta screens
            WireMetaScreenBack(_ui.Settings);
            WireMetaScreenBack(_ui.Leaderboard);
            WireMetaScreenBack(_ui.Profile);
            if (_ui.HeroesCollection != null)
            {
                _ui.HeroesCollection.OnBack -= ReturnToMainMenu;
                _ui.HeroesCollection.OnBack += ReturnToMainMenu;
            }
        }

        private void WireMetaScreenBack(UI.Toolkit.BackableScreenController screen)
        {
            if (screen == null) return;
            screen.OnBack -= ReturnToMainMenu;
            screen.OnBack += ReturnToMainMenu;
        }

        private void ShowHeroesCollection()
        {
            _heroPreview?.SetVisible(false);
            _heroSelectPreview?.SetVisible(false);
            if (_ui.HeroesCollection != null)
                _ui.ShowScreen(_ui.HeroesCollection);
        }

        private void ShowSettings()
        {
            _heroPreview?.SetVisible(false);
            if (_ui.Settings != null)
                _ui.ShowScreen(_ui.Settings);
        }

        private async void ShowLeaderboard()
        {
            _heroPreview?.SetVisible(false);
            if (_ui.Leaderboard == null) return;

            _ui.Leaderboard.SetLoading();
            _ui.ShowScreen(_ui.Leaderboard);

            try
            {
                var url = GetServerUrl();
                using var req = UnityEngine.Networking.UnityWebRequest.Get($"{url}/player/leaderboard");
                await req.SendWebRequest().ToUniTask();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    // Server returns array directly
                    var json = $"{{\"items\":{req.downloadHandler.text}}}";
                    var response = JsonUtility.FromJson<LeaderboardResponse>(json);
                    var list = new System.Collections.Generic.List<LeaderboardEntry>();
                    if (response?.items != null)
                        list.AddRange(response.items);
                    _ui.Leaderboard.SetData(list);
                }
                else
                {
                    _ui.Leaderboard.SetData(null);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameManager] Leaderboard fetch error: {ex.Message}");
                _ui.Leaderboard.SetData(null);
            }
        }

        private async void HandleConnectWallet()
        {
            var blockchain = ServiceLocator.Get<IBlockchainService>();
            if (blockchain == null)
            {
                _ui.Toasts?.ShowToast("Blockchain not available", ToastType.Warning);
                return;
            }

            if (blockchain.IsConnected)
            {
                blockchain.DisconnectWallet();
                UpdateWalletUI();
                _ui.Toasts?.ShowToast("Wallet disconnected", ToastType.Info);
                return;
            }

            try
            {
                _ui.Toasts?.ShowToast("Connecting wallet...", ToastType.Info);
                var address = await blockchain.ConnectWallet();
                if (!string.IsNullOrEmpty(address))
                {
                    UpdateWalletUI();
                    _ui.Toasts?.ShowToast($"Connected: {address[..6]}...", ToastType.Success);

                    // Save to server
                    try
                    {
                        var auth = ServiceLocator.Get<IPlatformAuth>();
                        var token = await auth.Authenticate();
                        if (!string.IsNullOrEmpty(token))
                        {
                            var url = GetServerUrl();
                            var body = $"{{\"walletAddress\":\"{address}\"}}";
                            using var req = new UnityEngine.Networking.UnityWebRequest($"{url}/blockchain/connect-wallet", "POST");
                            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                            req.SetRequestHeader("Content-Type", "application/json");
                            req.SetRequestHeader("Authorization", $"Bearer {token}");
                            await req.SendWebRequest().ToUniTask();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[GameManager] Failed to save wallet to server: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                _ui.Toasts?.ShowToast($"Wallet error: {ex.Message}", ToastType.Error);
            }
        }

        private void UpdateWalletUI()
        {
            var blockchain = ServiceLocator.Get<IBlockchainService>();
            if (_ui.MainMenu != null && blockchain != null)
                _ui.MainMenu.SetWalletStatus(blockchain.IsConnected, blockchain.WalletAddress);
        }

        private async void ShowProfile()
        {
            if (_ui.Profile == null) return;

            // Show screen immediately with placeholder
            _ui.Profile.SetOfflineProfile("Loading...", 0, 0);
            _ui.ShowScreen(_ui.Profile);

            // Try to fetch from server
            try
            {
                var auth = ServiceLocator.Get<IPlatformAuth>();
                var token = await auth.Authenticate();
                if (!string.IsNullOrEmpty(token))
                {
                    var url = GetServerUrl();
                    using var req = UnityEngine.Networking.UnityWebRequest.Get($"{url}/player/me");
                    req.SetRequestHeader("Authorization", $"Bearer {token}");
                    await req.SendWebRequest().ToUniTask();

                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<ServerProfileResponse>(req.downloadHandler.text);
                        _ui.Profile.SetProfile(response.ToProfileData());
                    }
                    else
                    {
                        Debug.LogWarning($"[GameManager] Profile fetch failed: {req.error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameManager] Profile fetch error: {ex.Message}");
                // Keep offline placeholder
            }
        }

        private async void HandlePlayOnline()
        {
            _ui.HideAll();
            if (_ui.Matchmaking != null)
            {
                _ui.ShowScreen(_ui.Matchmaking);
                _ui.Matchmaking.SetStatus("CONNECTING TO SERVER...");
            }

            try
            {
                var auth = ServiceLocator.Get<IPlatformAuth>();
                var token = await auth.Authenticate();

                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogError("[GameManager] Authentication failed — empty token");
                    _ui.Matchmaking?.SetStatus("AUTH FAILED. Open via Telegram.");
                    await UniTask.Delay(2000);
                    TransitionTo(FlowState.MainMenu);
                    return;
                }

                _socket?.Dispose();
                var url = GetServerUrl();
                _socket = new SocketIOClient(url);
                _socket.SetAuthToken(token);
                await _socket.ConnectAsync();

                StartOnlineGame(_socket);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] Online connection failed: {ex.Message}");
                _ui.Matchmaking?.SetStatus("CONNECTION FAILED");
                await UniTask.Delay(2000);
                TransitionTo(FlowState.MainMenu);
            }
        }

        private void EnterMatchmaking()
        {
            _ui.HideAll();
            if (_ui.Matchmaking != null)
            {
                _ui.ShowScreen(_ui.Matchmaking);
                _ui.Matchmaking.OnCancelMatchmaking -= HandleCancelMatchmaking;
                _ui.Matchmaking.OnCancelMatchmaking += HandleCancelMatchmaking;
            }
        }

        private void EnterHeroSelect()
        {
            _ui.HideAll();
            _heroPreview?.SetVisible(false);
            _heroSelectPreview?.SetVisible(true);
            SubscribeHeroSelect();

            if (_ui.HeroSelect != null)
            {
                // Fallback: ensure heroes are loaded (in case bootstrap timing failed)
                if (_ui.HeroSelect.GetHeroes() == null || _ui.HeroSelect.GetHeroes().Count == 0)
                {
                    var all = Resources.FindObjectsOfTypeAll<HeroConfig>();
                    var list = new List<HeroConfig>();
                    foreach (var h in all)
                        if (!string.IsNullOrEmpty(h.heroId)) list.Add(h);
                    list.Sort((a, b) => string.Compare(a.heroId, b.heroId, System.StringComparison.Ordinal));
                    _ui.HeroSelect.SetHeroes(list);
                    _ui.HeroesCollection?.SetHeroes(list);
                    Debug.LogWarning($"[GameManager] Fallback hero load: {list.Count} heroes");
                }

                bool isPassAndPlay = _offlineMode && !_vsBot;
                _ui.HeroSelect.Show(isPassAndPlay);
            }

            _ui.ShowScreen(_ui.HeroSelect);
        }

        private void EnterPreMatch()
        {
            _ui.HideAll();
            _ui.HideHUD();
            _heroPreview?.SetVisible(false);
            _heroSelectPreview?.SetVisible(false);

            if (_ui.PreMatch != null)
            {
                _ui.PreMatch.Show(_p1Hero, _allMaps, _vsBot, !_vsBot && _offlineMode);
                _ui.PreMatch.OnStartMatch -= HandlePreMatchStart;
                _ui.PreMatch.OnStartMatch += HandlePreMatchStart;
                _ui.PreMatch.OnBack -= HandlePreMatchBack;
                _ui.PreMatch.OnBack += HandlePreMatchBack;
                _ui.ShowScreen(_ui.PreMatch);
            }
            else
            {
                // Fallback if screen not available
                InitOfflineMatch();
                TransitionTo(FlowState.PlanningP1);
            }
        }

        private void HandlePreMatchStart()
        {
            if (_ui.PreMatch != null)
            {
                _ui.PreMatch.OnStartMatch -= HandlePreMatchStart;
                _ui.PreMatch.OnBack -= HandlePreMatchBack;

                // Apply settings
                var selectedMap = _ui.PreMatch.SelectedMap;
                if (selectedMap != null)
                    _activeMap = selectedMap;
            }

            if (_offlineMode)
            {
                InitOfflineMatch();

                // Apply shrink setting
                if (_matchManager != null)
                    _matchManager.EnableShrink = _ui.PreMatch?.ShrinkEnabled ?? true;

                TransitionTo(FlowState.PlanningP1);
            }
            else
            {
                // Online: proceed to matchmaking
                HandleOnlineHeroSelected(_p1Hero);
            }
        }

        private void HandlePreMatchBack()
        {
            if (_ui.PreMatch != null)
            {
                _ui.PreMatch.OnStartMatch -= HandlePreMatchStart;
                _ui.PreMatch.OnBack -= HandlePreMatchBack;
            }
            TransitionTo(FlowState.HeroSelect);
        }

        private void EnterPlanningP1()
        {
            _ui.HideAll();

            if (_ui.Planning != null)
            {
                string label = _isTutorial
                    ? "TUTORIAL — Plan your moves!"
                    : $"Player 1 --- {_p1Hero.displayName}";

                _ui.Planning.Show(_p1Hero, _matchManager.CurrentRound, label);

                if (_isTutorial)
                {
                    _ui.Planning.DisableTimer();
                    _ui.Toasts?.ShowToast("Tap MOVE, SHOOT, or other actions below!", ToastType.Info);
                }

                _ui.ShowScreen(_ui.Planning);
            }

            _gridView?.ClearPathPreview();
            SubscribePlanning();
            SubscribePathPreview();
        }

        private void EnterPlanningP2()
        {
            _ui.HideAll();

            if (_ui.Planning != null)
            {
                _ui.Planning.Show(_p2Hero, _matchManager.CurrentRound,
                    $"Player 2 --- {_p2Hero.displayName}");
                _ui.ShowScreen(_ui.Planning);
            }

            _gridView?.ClearPathPreview();

            SubscribePlanning();
            SubscribePathPreview();
        }

        private void EnterPlanningOnline()
        {
            _ui.HideAll();

            if (_ui.Planning != null)
            {
                _ui.Planning.Show(_p1Hero, _onlineRoundNumber, timeLimit: _onlineTimeLimit);
                _ui.ShowScreen(_ui.Planning);
            }

            SubscribePlanning();
        }

        private void EnterWaitingForOpponent()
        {
            _ui.HideAll();
            _ui.ShowHUD();
            _ui.Planning?.ShowWaitingOverlay("WAITING FOR OPPONENT...");
            if (_ui.Planning != null)
                _ui.ShowScreen(_ui.Planning);
            _waitingTimer = 0f;
        }

        private void EnterReveal()
        {
            if (_ui.Reveal != null && _p1Actions != null)
            {
                _ui.HideAll();
                _ui.ShowHUD();
                _ui.Reveal.OnRevealComplete -= HandleRevealComplete;
                _ui.Reveal.OnRevealComplete += HandleRevealComplete;
                _ui.Reveal.Show(_p1Actions, _lastP2Actions);
                _ui.ShowScreen(_ui.Reveal);
            }
            else
            {
                // Skip reveal if not available
                TransitionTo(FlowState.Execution);
            }
        }

        private void HandleRevealComplete()
        {
            if (_ui.Reveal != null)
                _ui.Reveal.OnRevealComplete -= HandleRevealComplete;
            TransitionTo(FlowState.Execution);
        }

        private void EnterExecution()
        {
            _ui.HideAll();
            _ui.ShowHUD();

            if (_offlineMode)
                PlayOfflineExecution();
        }

        private void EnterMatchResult()
        {
            _ui.HideAll();
            _ui.HideHUD();

            var result = GetMatchResult();
            int p1Wins = CountWins(true);
            int p2Wins = CountWins(false);

            SubscribeResult();

            if (_ui.Result != null)
            {
                _ui.Result.Show(result, _p1Hero, _p2Hero, p1Wins, p2Wins, _roundResults.ToArray());

                if (!_offlineMode && (_lastRatingDelta != 0 || _lastCoinsEarned != 0))
                    _ui.Result.ShowRewards(_lastRatingDelta, _lastCoinsEarned);
                else
                    _ui.Result.HideRewards();

                _ui.ShowScreen(_ui.Result);
            }
        }

        #endregion

        #region Offline Flow

        private void InitOfflineMatch()
        {
            _matchManager = new MatchManager();
            _matchManager.EnableShrink = true;

            // Use pre-selected map, or pick random, or use default
            if (_activeMap == null)
            {
                if (_allMaps != null && _allMaps.Length > 0)
                    _activeMap = _allMaps[Random.Range(0, _allMaps.Length)];
                else
                    _activeMap = _defaultMap;
            }
            _roundResults.Clear();

            _matchManager.StartMatch(_p1Hero, _p2Hero, _activeMap);

            if (_gridView != null)
                _gridView.RenderGrid(_matchManager.Grid, _activeMap.player1Spawn, _activeMap.player2Spawn);

            if (_ui.HUD != null)
            {
                _ui.HUD.Initialize(_p1Hero, _p2Hero);
                _ui.ShowHUD();
            }

            if (_cameraController != null && _gridView != null)
                _cameraController.FrameGrid(_gridView.GetGridCenter(), _gridView.GetGridExtent());

            if (_executionController != null)
            {
                // Set hero configs for per-hero projectiles
                _executionController.SetHeroConfigs(_p1Hero, _p2Hero);

                // Activate heroes before swap (Animator requires active object)
                _executionController.Hero1View?.gameObject.SetActive(true);
                _executionController.Hero2View?.gameObject.SetActive(true);

                // Swap 3D models if hero prefabs available
                if (_p1Hero != null && _p1Hero.heroPrefab != null && _executionController.Hero1View != null)
                    _executionController.Hero1View.SwapModel(_p1Hero.heroPrefab, _p1Hero.animatorController);
                if (_p2Hero != null && _p2Hero.heroPrefab != null && _executionController.Hero2View != null)
                    _executionController.Hero2View.SwapModel(_p2Hero.heroPrefab, _p2Hero.animatorController);

                _executionController.SetInitialPositions(
                    _activeMap.player1Spawn, _activeMap.player1Facing,
                    _activeMap.player2Spawn, _activeMap.player2Facing);
            }
        }

        private void SubmitOfflineP1(List<ActionType> actions)
        {
            _p1Actions = new List<ActionType>(actions);

            if (_vsBot)
            {
                // Bot generates P2 actions immediately
                var botActions = GenerateBotActions();
                SubmitOfflineP2(botActions);
            }
            else
            {
                _ui.Planning?.ShowPassDeviceOverlay("Pass the device\nto Player 2");
            }
        }

        private List<ActionType> GenerateBotActions()
        {
            if (_isTutorial)
            {
                var tutorialActions = TutorialBotAI.GetActions(_p2Hero.steps);
                Debug.Log($"[TutorialBot] Generated: {string.Join(", ", tutorialActions)}");
                return tutorialActions;
            }

            var p2State = _matchManager.GetPlayerState(1);
            var p1State = _matchManager.GetPlayerState(0);
            var grid = _matchManager.Grid;

            var bot = new BotAI(grid, p2State, p1State);
            var actions = bot.GenerateActions();

            Debug.Log($"[BotAI] Generated: {string.Join(", ", actions)}");
            return actions;
        }

        private void SubmitOfflineP2(List<ActionType> actions)
        {
            _lastP2Actions = new List<ActionType>(actions);
            UnsubscribePlanning();

            string p1Error = _matchManager.SubmitActions(0, _p1Actions);
            if (p1Error != null)
            {
                Debug.LogError($"[GameManager] P1 action validation failed: {p1Error}");
                TransitionTo(FlowState.PlanningP1);
                return;
            }

            string p2Error = _matchManager.SubmitActions(1, new List<ActionType>(actions));
            if (p2Error != null)
            {
                Debug.LogError($"[GameManager] P2 action validation failed: {p2Error}");
                TransitionTo(FlowState.PlanningP2);
                return;
            }

            TransitionTo(FlowState.Reveal);
        }

        private void PlayOfflineExecution()
        {
            var results = _matchManager.GetLastRoundResults();

            if (results == null || results.Count == 0)
            {
                Debug.LogWarning("[GameManager] No results to play back");
                HandlePlaybackComplete();
                return;
            }

            if (_executionController != null)
                _executionController.PlayRound(new List<StepResult>(results));
            else
                HandlePlaybackComplete();
        }

        private void HandlePlaybackComplete()
        {
            if (_offlineMode)
                HandleOfflinePlaybackComplete();
            else
                HandleOnlinePlaybackComplete();
        }

        private void HandleOfflinePlaybackComplete()
        {
            var phase = _matchManager.CurrentPhase;

            if (phase == GamePhase.PostMatch)
                TransitionTo(FlowState.MatchResult);
            else if (phase == GamePhase.Planning)
                TransitionTo(FlowState.PostRound);
            else
            {
                Debug.LogWarning($"[GameManager] Unexpected phase after playback: {phase}");
                TransitionTo(FlowState.MatchResult);
            }
        }

        private void EnterPostRound()
        {
            _ui.HideAll();
            _ui.ShowHUD();

            if (_ui.RoundTransition != null)
            {
                // Get last round result
                var lastResult = _roundResults.Count > 0
                    ? _roundResults[_roundResults.Count - 1]
                    : RoundResult.NoKill;

                int completedRound = _matchManager != null ? _matchManager.CurrentRound - 1 : 1;
                bool mapShrinking = _matchManager != null && _matchManager.EnableShrink;

                _ui.RoundTransition.OnTransitionComplete -= HandlePostRoundComplete;
                _ui.RoundTransition.OnTransitionComplete += HandlePostRoundComplete;
                _ui.RoundTransition.Show(completedRound, lastResult, mapShrinking, _p1Actions, _lastP2Actions);
                _ui.ShowScreen(_ui.RoundTransition);
            }
            else
            {
                // No transition overlay — skip directly
                TransitionTo(FlowState.PlanningP1);
            }
        }

        private void HandlePostRoundComplete()
        {
            if (_ui.RoundTransition != null)
                _ui.RoundTransition.OnTransitionComplete -= HandlePostRoundComplete;

            TransitionTo(FlowState.PlanningP1);
        }

        private void HandleOnlinePlaybackComplete()
        {
            if (_pendingMatchEnd != null)
            {
                var msg = _pendingMatchEnd;
                _pendingMatchEnd = null;
                _pendingRoundStart = null;
                _onlineWinner = msg.winner;
                TransitionTo(FlowState.MatchResult);
            }
            else if (_pendingRoundStart != null)
            {
                var msg = _pendingRoundStart;
                _pendingRoundStart = null;
                _onlineRoundNumber = msg.roundNumber;
                TransitionTo(FlowState.PlanningOnline);
            }
            else
            {
                Debug.Log("[GameManager] Playback done, waiting for server...");
                TransitionTo(FlowState.WaitingForOpponent);
            }
        }

        #endregion

        #region Online Flow

        private void HandleOnlineHeroSelected(HeroConfig hero)
        {
            _p1Hero = hero;
            _networkController?.FindMatch(hero.heroId);
            TransitionTo(FlowState.Matchmaking);
        }

        private void HandleMatchFound(MatchFoundMessage msg)
        {
            _currentMatchId = msg.matchId;
            var opponentHero = FindHeroById(msg.opponentHeroId);
            _p2Hero = opponentHero;
            _activeMap = _defaultMap;

            // Show opponent info on matchmaking screen
            if (_ui.Matchmaking != null)
            {
                string oppName = msg.opponentName ?? "Opponent";
                string oppHero = _p2Hero != null ? _p2Hero.displayName : msg.opponentHeroId;
                _ui.Matchmaking.SetStatus($"MATCHED!\n{oppName} — {oppHero}");
            }

            if (_ui.HUD != null)
            {
                _ui.HUD.Initialize(_p1Hero, _p2Hero);
                _ui.ShowHUD();
            }

            if (_gridView != null && _activeMap != null)
                _gridView.RenderGrid(
                    new GridSystem(_activeMap),
                    msg.yourSpawn.ToVector2Int(),
                    msg.opponentSpawn.ToVector2Int());

            if (_executionController != null)
                _executionController.SetInitialPositions(
                    msg.yourSpawn.ToVector2Int(), (Direction)msg.yourFacing,
                    msg.opponentSpawn.ToVector2Int(), (Direction)msg.opponentFacing);
        }

        private void HandleOnlineRoundStart(RoundStartMessage msg)
        {
            if (_currentState == FlowState.Execution)
            {
                _pendingRoundStart = msg;
                return;
            }

            _onlineRoundNumber = msg.roundNumber;
            _onlineTimeLimit = msg.timeLimit;
            TransitionTo(FlowState.PlanningOnline);
        }

        private void SubmitOnlineActions(List<ActionType> actions)
        {
            UnsubscribePlanning();

            _commitNonce = HashUtil.GenerateNonce();
            _commitHash = HashUtil.ComputeActionHash(actions.ToArray(), _commitNonce);

            _networkController?.CommitActions(_commitHash);
            TransitionTo(FlowState.WaitingForOpponent);
        }

        private void HandleBothCommitted()
        {
            if (_p1Actions != null && _networkController != null)
                _networkController.RevealActions(_p1Actions.ToArray(), _commitNonce);
        }

        private void HandleRoundResults(RoundResultsMessage msg)
        {
            var results = ConvertStepResults(msg.steps);
            TransitionTo(FlowState.Execution);

            if (_executionController != null)
                _executionController.PlayRound(results);
            else
                HandlePlaybackComplete();
        }

        private void HandleOnlineMatchEnd(MatchEndMessage msg)
        {
            _lastRatingDelta = msg.ratingDelta;
            _lastCoinsEarned = msg.coinsEarned;

            if (_currentState == FlowState.Execution)
            {
                _pendingMatchEnd = msg;
                return;
            }

            _onlineWinner = msg.winner;
            TransitionTo(FlowState.MatchResult);
        }

        private void HandleMatchError(string error)
        {
            Debug.LogError($"[GameManager] Match error: {error}");
            CleanupMatch();
            TransitionTo(FlowState.MainMenu);
        }

        #endregion

        #region Reconnection Flow

        private bool IsInOnlineMatch =>
            !_offlineMode && _currentState != FlowState.MainMenu
            && _currentState != FlowState.HeroSelect && _currentState != FlowState.MatchResult
            && _currentState != FlowState.PreMatch;

        private void HandleSocketDisconnectDuringMatch(string reason)
        {
            if (reason == "client_disconnect") return; // intentional
            if (!IsInOnlineMatch) return;

            Debug.LogWarning($"[GameManager] Socket disconnected during match: {reason}");
            _preReconnectState = _currentState;
            TransitionTo(FlowState.Reconnecting);
        }

        private void HandleSocketReconnected()
        {
            if (_currentState != FlowState.Reconnecting) return;

            Debug.Log("[GameManager] Socket reconnected — sending rejoin");
            _rejoinAttempts = 0;
            SendRejoin();
        }

        private void SendRejoin()
        {
            _rejoinAttempts++;
            _rejoinTimer = 0f;
            _awaitingRejoinAck = true;
            _networkController?.Rejoin();
            _ui.Reconnecting?.SetAttempt(_rejoinAttempts, MaxRejoinAttempts);
        }

        private void HandleSocketReconnectFailed(string error)
        {
            if (_currentState != FlowState.Reconnecting) return;

            if (error == "reconnect_failed")
            {
                Debug.LogError("[GameManager] All reconnect attempts exhausted");
                _ui.Reconnecting?.SetFailed();
            }
        }

        private void HandleReconnectAttempt(int attempt, int max)
        {
            if (_currentState != FlowState.Reconnecting) return;
            _ui.Reconnecting?.SetAttempt(attempt, max);
        }

        private void HandleRejoinAck(RejoinAckMessage msg)
        {
            _awaitingRejoinAck = false;

            if (!msg.success)
            {
                Debug.LogWarning($"[GameManager] Rejoin failed: {msg.error}");
                _ui.Toasts?.ShowToast("Match expired", ToastType.Error);
                CleanupMatch();
                TransitionTo(FlowState.MainMenu);
                return;
            }

            Debug.Log($"[GameManager] Rejoin successful — round {msg.state.currentRound}, phase {msg.state.phase}");
            _currentMatchId = msg.state.matchId;
            _onlineRoundNumber = msg.state.currentRound;

            // Restore hero refs if lost
            if (_p1Hero == null)
                _p1Hero = FindHeroById(msg.state.yourHeroId);
            if (_p2Hero == null)
                _p2Hero = FindHeroById(msg.state.opponentHeroId);

            // Re-show HUD
            if (_ui.HUD != null && _p1Hero != null && _p2Hero != null)
            {
                _ui.HUD.Initialize(_p1Hero, _p2Hero);
                _ui.ShowHUD();
            }

            // Sync grid positions from server state
            if (_executionController != null)
            {
                _executionController.SetInitialPositions(
                    msg.state.yourPos.ToVector2Int(), (Direction)msg.state.yourFacing,
                    msg.state.opponentPos.ToVector2Int(), (Direction)msg.state.opponentFacing);
            }

            // Resume to correct phase
            switch (msg.state.phase)
            {
                case "planning":
                    TransitionTo(FlowState.PlanningOnline);
                    break;
                case "committed":
                    // We already committed — wait for opponent
                    if (msg.state.hasCommitted)
                        TransitionTo(FlowState.WaitingForOpponent);
                    else
                        TransitionTo(FlowState.PlanningOnline);
                    break;
                default:
                    TransitionTo(FlowState.WaitingForOpponent);
                    break;
            }
        }

        private void HandleOpponentDisconnected(OpponentDisconnectedMessage msg)
        {
            Debug.Log($"[GameManager] Opponent disconnected (grace: {msg.gracePeriod}s)");
            _ui.Toasts?.ShowToast("Opponent disconnected...", ToastType.Warning);
        }

        private void HandleOpponentReconnectedNet()
        {
            Debug.Log("[GameManager] Opponent reconnected");
            _ui.Toasts?.ShowToast("Opponent reconnected!", ToastType.Info);
        }

        private void EnterReconnecting()
        {
            _ui.HideAll();
            _ui.HideHUD();

            if (_ui.Reconnecting != null)
            {
                _ui.Reconnecting.SetAttempt(1, 5);
                _ui.Reconnecting.OnCancelReconnect -= HandleCancelReconnect;
                _ui.Reconnecting.OnCancelReconnect += HandleCancelReconnect;
                _ui.ShowScreen(_ui.Reconnecting);
            }
        }

        private void HandleCancelReconnect()
        {
            if (_ui.Reconnecting != null)
                _ui.Reconnecting.OnCancelReconnect -= HandleCancelReconnect;

            // Forfeit: surrender if we can, then clean up
            _networkController?.Surrender();
            CleanupMatch();
            TransitionTo(FlowState.MainMenu);
        }

        #endregion

        #region UI Subscriptions

        private void SubscribeHeroSelect()
        {
            if (_ui.HeroSelect == null) return;
            _ui.HeroSelect.OnHeroesSelected += HandleHeroesSelected;
            _ui.HeroSelect.OnBackPressed += HandleHeroSelectBack;
        }

        private void UnsubscribeHeroSelect()
        {
            if (_ui.HeroSelect == null) return;
            _ui.HeroSelect.OnHeroesSelected -= HandleHeroesSelected;
            _ui.HeroSelect.OnBackPressed -= HandleHeroSelectBack;
        }

        private void SubscribePlanning()
        {
            if (_ui.Planning == null) return;
            _ui.Planning.OnActionsConfirmed += HandleActionsConfirmed;
            _ui.Planning.OnPassDeviceContinue += HandlePassDeviceContinue;
        }

        private void UnsubscribePlanning()
        {
            if (_ui.Planning == null) return;
            _ui.Planning.OnActionsConfirmed -= HandleActionsConfirmed;
            _ui.Planning.OnPassDeviceContinue -= HandlePassDeviceContinue;
            UnsubscribePathPreview();
            _gridView?.ClearPathPreview();
        }

        private void SubscribeResult()
        {
            if (_ui.Result == null) return;
            _ui.Result.OnRematchRequested += HandleRematch;
            _ui.Result.OnMainMenuRequested += HandleMainMenu;
        }

        private void UnsubscribeResult()
        {
            if (_ui.Result == null) return;
            _ui.Result.OnRematchRequested -= HandleRematch;
            _ui.Result.OnMainMenuRequested -= HandleMainMenu;
        }

        private void SubscribePathPreview()
        {
            // Unsubscribe first to prevent double-subscription
            GameEvents.OnActionQueued -= HandlePathPreviewUpdate;
            GameEvents.OnActionUndone -= HandlePathPreviewUndo;
            GameEvents.OnActionQueued += HandlePathPreviewUpdate;
            GameEvents.OnActionUndone += HandlePathPreviewUndo;
        }

        private void UnsubscribePathPreview()
        {
            GameEvents.OnActionQueued -= HandlePathPreviewUpdate;
            GameEvents.OnActionUndone -= HandlePathPreviewUndo;
        }

        private void HandlePathPreviewUpdate(int slotIndex, ActionType action)
        {
            UpdatePathPreview();
        }

        private void HandlePathPreviewUndo(int slotIndex)
        {
            UpdatePathPreview();
        }

        private void UpdatePathPreview()
        {
            if (_gridView == null || _ui.Planning == null || _matchManager == null) return;

            var queue = _ui.Planning.CurrentQueue;
            if (queue == null || queue.Count == 0)
            {
                _gridView.ClearPathPreview();
                return;
            }

            bool isP1 = _currentState == FlowState.PlanningP1 || _currentState == FlowState.PlanningOnline;
            var hero = isP1 ? _matchManager.Player1 : _matchManager.Player2;
            if (hero == null) return;

            _gridView.ShowPathPreview(hero.Position, hero.Facing,
                new List<ActionType>(queue), isP1);
        }

        private void UnsubscribeUI()
        {
            UnsubscribeHeroSelect();
            UnsubscribePlanning();
            UnsubscribeResult();
            UnsubscribePathPreview();
        }

        #endregion

        #region Network Subscriptions

        private void SubscribeNetwork()
        {
            if (_networkController == null) return;
            _networkController.OnMatchFound += HandleMatchFound;
            _networkController.OnRoundStart += HandleOnlineRoundStart;
            _networkController.OnBothCommitted += HandleBothCommitted;
            _networkController.OnRoundResults += HandleRoundResults;
            _networkController.OnMatchEnded += HandleOnlineMatchEnd;
            _networkController.OnMatchError += HandleMatchError;
            _networkController.OnRejoinAck += HandleRejoinAck;
            _networkController.OnOpponentDisconnected += HandleOpponentDisconnected;
            _networkController.OnOpponentReconnected += HandleOpponentReconnectedNet;

            // Socket-level disconnect triggers reconnecting flow during active match
            if (_socket != null)
            {
                _socket.OnDisconnected -= HandleSocketDisconnectDuringMatch;
                _socket.OnDisconnected += HandleSocketDisconnectDuringMatch;
                _socket.OnConnected -= HandleSocketReconnected;
                _socket.OnConnected += HandleSocketReconnected;
                _socket.OnError -= HandleSocketReconnectFailed;
                _socket.OnError += HandleSocketReconnectFailed;
                _socket.OnReconnectAttempt -= HandleReconnectAttempt;
                _socket.OnReconnectAttempt += HandleReconnectAttempt;
            }
        }

        private void UnsubscribeNetwork()
        {
            if (_networkController == null) return;
            _networkController.OnMatchFound -= HandleMatchFound;
            _networkController.OnRoundStart -= HandleOnlineRoundStart;
            _networkController.OnBothCommitted -= HandleBothCommitted;
            _networkController.OnRoundResults -= HandleRoundResults;
            _networkController.OnMatchEnded -= HandleOnlineMatchEnd;
            _networkController.OnMatchError -= HandleMatchError;
            _networkController.OnRejoinAck -= HandleRejoinAck;
            _networkController.OnOpponentDisconnected -= HandleOpponentDisconnected;
            _networkController.OnOpponentReconnected -= HandleOpponentReconnectedNet;

            if (_socket != null)
            {
                _socket.OnDisconnected -= HandleSocketDisconnectDuringMatch;
                _socket.OnConnected -= HandleSocketReconnected;
                _socket.OnError -= HandleSocketReconnectFailed;
                _socket.OnReconnectAttempt -= HandleReconnectAttempt;
            }
        }

        #endregion

        #region UI Event Handlers

        private void HandleHeroesSelected(HeroConfig p1Hero, HeroConfig p2Hero)
        {
            UnsubscribeHeroSelect();

            if (_offlineMode)
            {
                _p1Hero = p1Hero;

                if (_vsBot)
                {
                    _p2Hero = PickBotHero(p1Hero);
                    Debug.Log($"[BotAI] Bot chose: {_p2Hero.displayName}");
                }
                else
                {
                    _p2Hero = p2Hero;
                }

                // Go to pre-match settings (or skip if tutorial)
                if (_isTutorial)
                {
                    InitOfflineMatch();
                    TransitionTo(FlowState.PlanningP1);
                }
                else
                {
                    TransitionTo(FlowState.PreMatch);
                }
            }
            else
            {
                // Online: show PreMatch settings before matchmaking
                _p1Hero = p1Hero;
                TransitionTo(FlowState.PreMatch);
            }
        }

        private HeroConfig PickBotHero(HeroConfig exclude)
        {
            var heroes = _ui.HeroSelect?.GetHeroes();
            if (heroes == null || heroes.Count <= 1) return exclude;

            // Prefer a hero that counters the player's pick
            var candidates = heroes.FindAll(h => h != exclude && !string.IsNullOrEmpty(h.heroId));
            if (candidates.Count == 0) return exclude;

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void HandleHeroSelectBack()
        {
            UnsubscribeHeroSelect();
            ReturnToMainMenu();
        }

        private void HandleActionsConfirmed(List<ActionType> actions)
        {
            switch (_currentState)
            {
                case FlowState.PlanningP1:
                    SubmitOfflineP1(actions);
                    break;
                case FlowState.PlanningP2:
                    SubmitOfflineP2(actions);
                    break;
                case FlowState.PlanningOnline:
                    _p1Actions = new List<ActionType>(actions);
                    SubmitOnlineActions(actions);
                    break;
            }
        }

        private void HandlePassDeviceContinue()
        {
            UnsubscribePlanning();
            TransitionTo(FlowState.PlanningP2);
        }

        private void HandleRematch()
        {
            UnsubscribeResult();

            if (_isTutorial)
            {
                // Tutorial complete — go to main menu instead of rematch
                _isTutorial = false;
                PlayerPrefs.SetInt("tutorial_done", 1);
                PlayerPrefs.Save();
                ReturnToMainMenu();
                return;
            }

            if (_offlineMode)
            {
                InitOfflineMatch();
                TransitionTo(FlowState.PlanningP1);
            }
            else
            {
                TransitionTo(FlowState.HeroSelect);
            }
        }

        private void HandleMainMenu()
        {
            UnsubscribeResult();

            if (_isTutorial)
            {
                _isTutorial = false;
                PlayerPrefs.SetInt("tutorial_done", 1);
                PlayerPrefs.Save();
                Debug.Log("[Tutorial] Completed! Marked as done.");
            }

            ReturnToMainMenu();
        }

        private void HandleCancelMatchmaking()
        {
            if (_ui.Matchmaking != null)
                _ui.Matchmaking.OnCancelMatchmaking -= HandleCancelMatchmaking;

            CleanupMatch();
            TransitionTo(FlowState.MainMenu);
        }

        #endregion

        #region GameEvents Tracking

        private void CaptureRoundResult(RoundResult result)
        {
            _roundResults.Add(result);
        }

        #endregion

        #region Helpers

        private string GetServerUrl()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Platform.WebGL.WebGLConfig.GetServerUrl();
#else
            return _serverUrl;
#endif
        }

        private void CleanupMatch()
        {
            _matchManager = null;
            _p1Hero = null;
            _p2Hero = null;
            _p1Actions = null;
            _activeMap = null;
            _commitHash = null;
            _commitNonce = null;
            _onlineWinner = -1;
            _onlineRoundNumber = 1;
            _pendingRoundStart = null;
            _pendingMatchEnd = null;
            _currentMatchId = null;
            _lastRatingDelta = 0;
            _lastCoinsEarned = 0;
            _roundResults.Clear();
            UnsubscribeUI();
            UnsubscribeNetwork();
            _networkController?.Dispose();
            _networkController = null;
            _socket?.Dispose();
            _socket = null;

            GameEvents.ClearAll();
        }

        private MatchResult GetMatchResult()
        {
            if (!_offlineMode && _onlineWinner >= 0)
            {
                return _onlineWinner switch
                {
                    0 => MatchResult.Player1Win,
                    1 => MatchResult.Player2Win,
                    _ => MatchResult.Draw
                };
            }

            if (_matchManager != null)
                return InferResultFromRounds();

            return MatchResult.Draw;
        }

        private MatchResult InferResultFromRounds()
        {
            int p1 = CountWins(true);
            int p2 = CountWins(false);
            if (p1 > p2) return MatchResult.Player1Win;
            if (p2 > p1) return MatchResult.Player2Win;
            return MatchResult.Draw;
        }

        private int CountWins(bool isPlayer1)
        {
            int count = 0;
            foreach (var r in _roundResults)
            {
                if (isPlayer1 && r == RoundResult.Player1Kill) count++;
                if (!isPlayer1 && r == RoundResult.Player2Kill) count++;
            }
            return count;
        }

        private static T SafeCast<T>(int value) where T : struct, System.Enum
        {
            if (System.Enum.IsDefined(typeof(T), value))
                return (T)(object)value;
            Debug.LogWarning($"[GameManager] Invalid enum value {value} for {typeof(T).Name}, using default");
            return default;
        }

        private static PickupType ParsePickup(string value)
        {
            if (string.IsNullOrEmpty(value)) return PickupType.None;
            if (int.TryParse(value, out int i) && System.Enum.IsDefined(typeof(PickupType), i))
                return (PickupType)i;
            return PickupType.None;
        }

        private HeroConfig FindHeroById(string heroId)
        {
            var heroes = Resources.FindObjectsOfTypeAll<HeroConfig>();
            foreach (var h in heroes)
            {
                if (h.heroId == heroId) return h;
            }
            Debug.LogWarning($"[GameManager] Hero not found: {heroId}");
            return null;
        }

        private static List<StepResult> ConvertStepResults(StepResultData[] data)
        {
            if (data == null) return new List<StepResult>();

            var results = new List<StepResult>(data.Length);
            foreach (var d in data)
            {
                results.Add(new StepResult
                {
                    StepIndex = d.stepIndex,
                    P1Action = SafeCast<ActionType>(d.p1Action),
                    P1StartPos = d.p1StartPos,
                    P1EndPos = d.p1EndPos,
                    P1StartFacing = SafeCast<Direction>(d.p1StartFacing),
                    P1EndFacing = SafeCast<Direction>(d.p1EndFacing),
                    P2Action = SafeCast<ActionType>(d.p2Action),
                    P2StartPos = d.p2StartPos,
                    P2EndPos = d.p2EndPos,
                    P2StartFacing = SafeCast<Direction>(d.p2StartFacing),
                    P2EndFacing = SafeCast<Direction>(d.p2EndFacing),
                    P1Fired = d.p1Fired,
                    P2Fired = d.p2Fired,
                    P1Hit = d.p1Hit,
                    P2Hit = d.p2Hit,
                    MutualCancel = d.mutualCancel,
                    P1ArmorBroken = d.p1ArmorBroken,
                    P2ArmorBroken = d.p2ArmorBroken,
                    P1Eliminated = d.p1Eliminated,
                    P2Eliminated = d.p2Eliminated,
                    P1Shielded = d.p1Shielded,
                    P2Shielded = d.p2Shielded,
                    P1PickedUp = ParsePickup(d.p1PickedUp),
                    P2PickedUp = ParsePickup(d.p2PickedUp)
                });
            }
            return results;
        }

        #endregion
    }
}
