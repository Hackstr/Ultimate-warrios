using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;
using TacticalDuelist.Core.Utils;
using TacticalDuelist.Networking;
using TacticalDuelist.Platform;
using TacticalDuelist.UI;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Main scene entry point. Wires MatchManager (pure logic), UI screens,
    /// ExecutionController (visual playback), GridView, and optional networking.
    /// Manages the full game flow state machine independently of MatchManager's GamePhase.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Flow State

        public enum FlowState
        {
            MainMenu,
            Matchmaking,
            HeroSelect,
            PlanningP1,
            PlanningP2,
            PlanningOnline,
            WaitingForOpponent,
            Execution,
            MatchResult
        }

        #endregion

        #region Serialized Fields

        [Header("UI Screens")]
        [SerializeField] private MainMenuScreen _mainMenuScreen;
        [SerializeField] private HeroSelectScreen _heroSelectScreen;
        [SerializeField] private MatchmakingScreen _matchmakingScreen;
        [SerializeField] private PlanningScreen _planningScreen;
        [SerializeField] private ResultScreen _resultScreen;
        [SerializeField] private HUD _hud;

        [Header("Gameplay")]
        [SerializeField] private ExecutionController _executionController;
        [SerializeField] private GridView _gridView;
        [SerializeField] private CameraController _cameraController;

        [Header("Default Map (offline)")]
        [SerializeField] private MapConfig _defaultMap;

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

        private HeroConfig _p1Hero;
        private HeroConfig _p2Hero;
        private MapConfig _activeMap;

        private List<ActionType> _p1Actions;
        private string _commitHash;
        private string _commitNonce;

        private readonly List<RoundResult> _roundResults = new();
        private int _onlineWinner = -1; // 0=P1, 1=P2, 2=Draw, -1=not set
        private int _onlineRoundNumber = 1;

        private RoundStartMessage _pendingRoundStart;
        private MatchEndMessage _pendingMatchEnd;

        #endregion

        #region Properties

        public FlowState CurrentState => _currentState;
        public bool IsOffline => _offlineMode;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            HideAllScreens();
            TransitionTo(FlowState.MainMenu);
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
        /// Starts a new game in offline (pass-and-play) mode.
        /// Called from main menu button or external trigger.
        /// </summary>
        public void StartOfflineGame()
        {
            _offlineMode = true;
            _networkController?.Dispose();
            _networkController = null;
            TransitionTo(FlowState.HeroSelect);
        }

        /// <summary>
        /// Starts a new game in online mode.
        /// Requires an active SocketIOClient connection.
        /// </summary>
        public void StartOnlineGame(SocketIOClient socket)
        {
            _offlineMode = false;
            _networkController?.Dispose();
            _networkController = new MatchNetworkController(socket);
            SubscribeNetwork();
            TransitionTo(FlowState.HeroSelect);
        }

        /// <summary>
        /// Returns to main menu, cleaning up current match state.
        /// </summary>
        public void ReturnToMainMenu()
        {
            CleanupMatch();
            TransitionTo(FlowState.MainMenu);
        }

        /// <summary>
        /// Called by GameBootstrap to wire runtime-created references.
        /// </summary>
        public void SetupRuntime(
            MainMenuScreen mainMenu,
            HeroSelectScreen heroSelect,
            MatchmakingScreen matchmaking,
            PlanningScreen planning,
            ResultScreen result,
            HUD hud,
            ExecutionController execution,
            GridView gridView,
            CameraController cameraController,
            MapConfig defaultMap)
        {
            _mainMenuScreen = mainMenu;
            _heroSelectScreen = heroSelect;
            _matchmakingScreen = matchmaking;
            _planningScreen = planning;
            _resultScreen = result;
            _hud = hud;
            _executionController = execution;
            _gridView = gridView;
            _cameraController = cameraController;
            _defaultMap = defaultMap;
            _offlineMode = true;

            if (_executionController != null)
            {
                _executionController.OnPlaybackComplete -= HandlePlaybackComplete;
                _executionController.OnPlaybackComplete += HandlePlaybackComplete;
            }
        }

        #endregion

        #region State Machine

        private void TransitionTo(FlowState newState)
        {
            Debug.Log($"[GameManager] {_currentState} → {newState}");
            _currentState = newState;

            switch (newState)
            {
                case FlowState.MainMenu:
                    EnterMainMenu();
                    break;
                case FlowState.Matchmaking:
                    EnterMatchmaking();
                    break;
                case FlowState.HeroSelect:
                    EnterHeroSelect();
                    break;
                case FlowState.PlanningP1:
                    EnterPlanningP1();
                    break;
                case FlowState.PlanningP2:
                    EnterPlanningP2();
                    break;
                case FlowState.PlanningOnline:
                    EnterPlanningOnline();
                    break;
                case FlowState.WaitingForOpponent:
                    EnterWaitingForOpponent();
                    break;
                case FlowState.Execution:
                    EnterExecution();
                    break;
                case FlowState.MatchResult:
                    EnterMatchResult();
                    break;
            }
        }

        #endregion

        #region State Entries

        private void EnterMainMenu()
        {
            HideAllScreens();
            _hud?.Hide();

            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.Show();
                _mainMenuScreen.OnPlayOffline -= StartOfflineGame;
                _mainMenuScreen.OnPlayOffline += StartOfflineGame;
                _mainMenuScreen.OnPlayOnline -= HandlePlayOnline;
                _mainMenuScreen.OnPlayOnline += HandlePlayOnline;
            }
        }

        private async void HandlePlayOnline()
        {
            HideAllScreens();
            _matchmakingScreen?.Show();
            _matchmakingScreen?.SetStatus("CONNECTING TO SERVER...");

            try
            {
                var auth = ServiceLocator.Get<IPlatformAuth>();
                var token = await auth.Authenticate();

                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogError("[GameManager] Authentication failed — empty token");
                    _matchmakingScreen?.SetStatus("AUTH FAILED. Open via Telegram.");
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
                _matchmakingScreen?.SetStatus("CONNECTION FAILED");
                await UniTask.Delay(2000);
                TransitionTo(FlowState.MainMenu);
            }
        }

        private void EnterMatchmaking()
        {
            HideAllScreens();

            if (_matchmakingScreen != null)
            {
                _matchmakingScreen.Show();
                _matchmakingScreen.OnCancelMatchmaking -= HandleCancelMatchmaking;
                _matchmakingScreen.OnCancelMatchmaking += HandleCancelMatchmaking;
            }
        }

        private void EnterHeroSelect()
        {
            HideAllScreens();
            SubscribeHeroSelect();

            if (_heroSelectScreen != null)
                _heroSelectScreen.Show(_offlineMode);
        }

        private void EnterPlanningP1()
        {
            HideAllScreens();
            _hud?.Show();

            if (_planningScreen != null)
                _planningScreen.Show(_p1Hero, _matchManager.CurrentRound,
                    $"Player 1 — {_p1Hero.displayName}");

            SubscribePlanning();
        }

        private void EnterPlanningP2()
        {
            HideAllScreens();
            _hud?.Show();

            if (_planningScreen != null)
                _planningScreen.Show(_p2Hero, _matchManager.CurrentRound,
                    $"Player 2 — {_p2Hero.displayName}");

            SubscribePlanning();
        }

        private void EnterPlanningOnline()
        {
            HideAllScreens();
            _hud?.Show();

            if (_planningScreen != null)
                _planningScreen.Show(_p1Hero, _onlineRoundNumber);

            SubscribePlanning();
        }

        private void EnterWaitingForOpponent()
        {
            HideAllScreens();
            _hud?.Show();
            _planningScreen?.ShowWaitingOverlay("WAITING FOR OPPONENT...");
        }

        private void EnterExecution()
        {
            HideAllScreens();
            _hud?.Show();

            if (_offlineMode)
                PlayOfflineExecution();
        }

        private void EnterMatchResult()
        {
            HideAllScreens();
            _hud?.Hide();

            var result = GetMatchResult();
            int p1Wins = CountWins(true);
            int p2Wins = CountWins(false);

            SubscribeResult();

            if (_resultScreen != null)
                _resultScreen.Show(result, _p1Hero, _p2Hero, p1Wins, p2Wins, _roundResults.ToArray());
        }

        #endregion

        #region Offline Flow (T-071)

        private void InitOfflineMatch()
        {
            _matchManager = new MatchManager();
            _matchManager.EnableShrink = false; // disabled for testing
            _activeMap = _defaultMap;
            _roundResults.Clear();

            _matchManager.StartMatch(_p1Hero, _p2Hero, _activeMap);

            if (_gridView != null)
                _gridView.RenderGrid(_matchManager.Grid, _activeMap.player1Spawn, _activeMap.player2Spawn);

            if (_hud != null)
            {
                _hud.Initialize(_p1Hero, _p2Hero);
                _hud.Show();
            }

            if (_cameraController != null && _gridView != null)
                _cameraController.FrameGrid(_gridView.GetGridCenter(), _gridView.GetGridExtent());

            if (_executionController != null)
                _executionController.SetInitialPositions(
                    _activeMap.player1Spawn, _activeMap.player1Facing,
                    _activeMap.player2Spawn, _activeMap.player2Facing);
        }

        private void SubmitOfflineP1(List<ActionType> actions)
        {
            _p1Actions = new List<ActionType>(actions);

            _planningScreen?.ShowPassDeviceOverlay(
                "Pass the device\nto Player 2");
        }

        private void SubmitOfflineP2(List<ActionType> actions)
        {
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

            // MatchManager has resolved the round synchronously by now.
            // Capture round result from MatchManager's state.
            TransitionTo(FlowState.Execution);
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
            {
                HandleOfflinePlaybackComplete();
            }
            else
            {
                HandleOnlinePlaybackComplete();
            }
        }

        private void HandleOfflinePlaybackComplete()
        {
            var phase = _matchManager.CurrentPhase;

            if (phase == GamePhase.PostMatch)
            {
                TransitionTo(FlowState.MatchResult);
            }
            else if (phase == GamePhase.Planning)
            {
                TransitionTo(FlowState.PlanningP1);
            }
            else
            {
                Debug.LogWarning($"[GameManager] Unexpected phase after playback: {phase}");
                TransitionTo(FlowState.MatchResult);
            }
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
                Debug.Log("[GameManager] Playback done, waiting for server round:start or match:end...");
                TransitionTo(FlowState.WaitingForOpponent);
            }
        }

        #endregion

        #region Online Flow (T-072)

        private void HandleOnlineHeroSelected(HeroConfig hero)
        {
            _p1Hero = hero;

            if (_networkController != null)
                _networkController.FindMatch(hero.heroId);

            TransitionTo(FlowState.Matchmaking);
        }

        private void HandleMatchFound(MatchFoundMessage msg)
        {
            var opponentHero = FindHeroById(msg.opponentHeroId);
            _p2Hero = opponentHero;

            _activeMap = _defaultMap;

            if (_hud != null)
            {
                _hud.Initialize(_p1Hero, _p2Hero);
                _hud.Show();
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
            // Server says both committed — reveal our actions
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

        #region UI Subscriptions

        private void SubscribeHeroSelect()
        {
            if (_heroSelectScreen == null) return;

            _heroSelectScreen.OnHeroesSelected += HandleHeroesSelected;
            _heroSelectScreen.OnBackPressed += HandleHeroSelectBack;
        }

        private void UnsubscribeHeroSelect()
        {
            if (_heroSelectScreen == null) return;

            _heroSelectScreen.OnHeroesSelected -= HandleHeroesSelected;
            _heroSelectScreen.OnBackPressed -= HandleHeroSelectBack;
        }

        private void SubscribePlanning()
        {
            if (_planningScreen == null) return;
            _planningScreen.OnActionsConfirmed += HandleActionsConfirmed;
            _planningScreen.OnPassDeviceContinue += HandlePassDeviceContinue;
        }

        private void UnsubscribePlanning()
        {
            if (_planningScreen == null) return;
            _planningScreen.OnActionsConfirmed -= HandleActionsConfirmed;
            _planningScreen.OnPassDeviceContinue -= HandlePassDeviceContinue;
        }

        private void SubscribeResult()
        {
            if (_resultScreen == null) return;

            _resultScreen.OnRematchRequested += HandleRematch;
            _resultScreen.OnMainMenuRequested += HandleMainMenu;
        }

        private void UnsubscribeResult()
        {
            if (_resultScreen == null) return;

            _resultScreen.OnRematchRequested -= HandleRematch;
            _resultScreen.OnMainMenuRequested -= HandleMainMenu;
        }

        private void UnsubscribeUI()
        {
            UnsubscribeHeroSelect();
            UnsubscribePlanning();
            UnsubscribeResult();
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
        }

        #endregion

        #region UI Event Handlers

        private void HandleHeroesSelected(HeroConfig p1Hero, HeroConfig p2Hero)
        {
            UnsubscribeHeroSelect();
            _heroSelectScreen?.Hide();

            if (_offlineMode)
            {
                _p1Hero = p1Hero;
                _p2Hero = p2Hero;
                InitOfflineMatch();
                TransitionTo(FlowState.PlanningP1);
            }
            else
            {
                HandleOnlineHeroSelected(p1Hero);
            }
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
            _resultScreen?.Hide();

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
            ReturnToMainMenu();
        }

        private void HandleCancelMatchmaking()
        {
            if (_matchmakingScreen != null)
                _matchmakingScreen.OnCancelMatchmaking -= HandleCancelMatchmaking;

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

        private void HideAllScreens()
        {
            _mainMenuScreen?.Hide();
            _heroSelectScreen?.Hide();
            _matchmakingScreen?.Hide();
            _planningScreen?.Hide();
            _resultScreen?.Hide();
        }

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
            _roundResults.Clear();
            UnsubscribeUI();
            UnsubscribeNetwork();
            _networkController?.Dispose();
            _networkController = null;
            _socket?.Dispose();
            _socket = null;

            if (_matchmakingScreen != null)
                _matchmakingScreen.OnCancelMatchmaking -= HandleCancelMatchmaking;

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

        private HeroConfig FindHeroById(string heroId)
        {
            if (_heroSelectScreen == null) return null;

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
                    P1Action = (ActionType)d.p1Action,
                    P1StartPos = d.p1StartPos,
                    P1EndPos = d.p1EndPos,
                    P1StartFacing = (Direction)d.p1StartFacing,
                    P1EndFacing = (Direction)d.p1EndFacing,
                    P2Action = (ActionType)d.p2Action,
                    P2StartPos = d.p2StartPos,
                    P2EndPos = d.p2EndPos,
                    P2StartFacing = (Direction)d.p2StartFacing,
                    P2EndFacing = (Direction)d.p2EndFacing,
                    P1Fired = d.p1Fired,
                    P2Fired = d.p2Fired,
                    P1Hit = d.p1Hit,
                    P2Hit = d.p2Hit,
                    MutualCancel = d.mutualCancel,
                    P1ArmorBroken = d.p1ArmorBroken,
                    P2ArmorBroken = d.p2ArmorBroken,
                    P1Eliminated = d.p1Eliminated,
                    P2Eliminated = d.p2Eliminated
                });
            }
            return results;
        }

        #endregion
    }
}
