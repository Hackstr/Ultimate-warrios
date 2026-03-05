using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.Testing
{
    /// <summary>
    /// Bypasses all UI — runs a hardcoded match and plays back the execution visually.
    /// Attach to a scene with GridView, ExecutionController, and CameraController.
    /// Useful for verifying that resolution + 3D playback work correctly end-to-end.
    /// </summary>
    public class QuickMatchTest : MonoBehaviour
    {
        #region Fields

        [Header("Heroes")]
        [SerializeField] private HeroConfig _p1Hero;
        [SerializeField] private HeroConfig _p2Hero;

        [Header("Map")]
        [SerializeField] private MapConfig _map;

        [Header("Scene References")]
        [SerializeField] private Gameplay.GridView _gridView;
        [SerializeField] private Gameplay.ExecutionController _executionController;
        [SerializeField] private Gameplay.CameraController _cameraController;

        [Header("Test Actions")]
        [Tooltip("P1 actions as comma-separated ActionType names: Move,Move,Shoot,TurnRight,Move,Shoot")]
        [SerializeField] private string _p1ActionsStr = "Move,Move,Shoot,TurnRight,Move,Shoot";
        [Tooltip("P2 actions: Move,TurnLeft,Move,Shoot,Move,Shoot")]
        [SerializeField] private string _p2ActionsStr = "Move,TurnLeft,Move,Shoot,Move,Shoot";

        [Header("Options")]
        [SerializeField] private bool _autoRunOnStart = true;
        [SerializeField] private float _playbackSpeed = 1f;

        private MatchManager _matchManager;
        private bool _isRunning;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_autoRunOnStart)
                RunTest();
        }

        private void OnDestroy()
        {
            if (_executionController != null)
                _executionController.OnPlaybackComplete -= OnPlaybackDone;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Call from Inspector button or code to execute the test match.
        /// </summary>
        [ContextMenu("Run Test Match")]
        public void RunTest()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[QuickMatchTest] Already running.");
                return;
            }

            if (_p1Hero == null || _p2Hero == null || _map == null)
            {
                Debug.LogError("[QuickMatchTest] Missing hero or map assignment.");
                return;
            }

            _isRunning = true;

            var p1Actions = ParseActions(_p1ActionsStr, _p1Hero.steps);
            var p2Actions = ParseActions(_p2ActionsStr, _p2Hero.steps);

            Debug.Log($"[QuickMatchTest] Starting: {_p1Hero.heroName} vs {_p2Hero.heroName}");
            Debug.Log($"[QuickMatchTest] P1 actions: {string.Join(", ", p1Actions)}");
            Debug.Log($"[QuickMatchTest] P2 actions: {string.Join(", ", p2Actions)}");

            _matchManager = new MatchManager();
            _matchManager.StartMatch(_p1Hero, _p2Hero, _map);

            // Render the grid
            if (_gridView != null)
            {
                _gridView.RenderGrid(
                    _matchManager.Grid,
                    _map.player1Spawn,
                    _map.player2Spawn
                );
            }

            // Set up camera
            if (_cameraController != null && _gridView != null)
            {
                _cameraController.SetTarget(_gridView.GetGridCenter());
                _cameraController.SetDistance(_gridView.GetGridExtent() * 1.5f);
            }

            // Submit both players' actions
            string err1 = _matchManager.SubmitActions(0, p1Actions);
            if (!string.IsNullOrEmpty(err1))
            {
                Debug.LogError($"[QuickMatchTest] P1 submit error: {err1}");
                _isRunning = false;
                return;
            }

            string err2 = _matchManager.SubmitActions(1, p2Actions);
            if (!string.IsNullOrEmpty(err2))
            {
                Debug.LogError($"[QuickMatchTest] P2 submit error: {err2}");
                _isRunning = false;
                return;
            }

            // Round is now resolved — get results and play back
            var results = _matchManager.GetLastRoundResults();
            Debug.Log($"[QuickMatchTest] Round resolved: {results.Count} steps");

            LogStepResults(results);

            if (_executionController != null)
            {
                _executionController.OnPlaybackComplete += OnPlaybackDone;
                _executionController.SetPlaybackSpeed(_playbackSpeed);
                _executionController.PlayRound(new List<StepResult>(results));
            }
            else
            {
                Debug.Log("[QuickMatchTest] No ExecutionController — results logged only.");
                _isRunning = false;
            }
        }

        #endregion

        #region Private Methods

        private void OnPlaybackDone()
        {
            _executionController.OnPlaybackComplete -= OnPlaybackDone;
            _isRunning = false;

            var phase = _matchManager.CurrentPhase;
            Debug.Log($"[QuickMatchTest] Playback complete. MatchManager phase: {phase}");

            if (phase == GamePhase.PostMatch)
            {
                Debug.Log("[QuickMatchTest] MATCH OVER.");
            }
            else
            {
                Debug.Log("[QuickMatchTest] Round done — next round pending. " +
                          "Re-run with new actions or call RunTest() again.");
            }
        }

        private static List<ActionType> ParseActions(string input, int heroSteps)
        {
            var actions = new List<ActionType>(heroSteps);

            if (string.IsNullOrWhiteSpace(input))
            {
                for (int i = 0; i < heroSteps; i++)
                    actions.Add(ActionType.Wait);
                return actions;
            }

            var parts = input.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (System.Enum.TryParse<ActionType>(trimmed, true, out var action))
                    actions.Add(action);
                else
                    Debug.LogWarning($"[QuickMatchTest] Unknown action '{trimmed}', using Wait");
            }

            while (actions.Count < heroSteps)
                actions.Add(ActionType.Wait);

            if (actions.Count > heroSteps)
                actions.RemoveRange(heroSteps, actions.Count - heroSteps);

            return actions;
        }

        private static void LogStepResults(IReadOnlyList<StepResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var s = results[i];
                var sb = new System.Text.StringBuilder();
                sb.Append($"  Step {i}: ");
                sb.Append($"P1[{s.P1Action}] {s.P1StartPos}→{s.P1EndPos} ");
                sb.Append($"P2[{s.P2Action}] {s.P2StartPos}→{s.P2EndPos}");

                if (s.P1Fired) sb.Append(" | P1 FIRED");
                if (s.P2Fired) sb.Append(" | P2 FIRED");
                if (s.P1Hit) sb.Append(" | P1 HIT!");
                if (s.P2Hit) sb.Append(" | P2 HIT!");
                if (s.MutualCancel) sb.Append(" | MUTUAL CANCEL");
                if (s.P1ArmorBroken) sb.Append(" | P1 ARMOR BROKEN");
                if (s.P2ArmorBroken) sb.Append(" | P2 ARMOR BROKEN");
                if (s.P1Eliminated) sb.Append(" | P1 ELIMINATED");
                if (s.P2Eliminated) sb.Append(" | P2 ELIMINATED");

                Debug.Log(sb.ToString());
            }
        }

        #endregion
    }
}
