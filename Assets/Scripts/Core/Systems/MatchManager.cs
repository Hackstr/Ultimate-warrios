using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Orchestrates the full match lifecycle: hero select → rounds → result.
    /// Pure game logic — no MonoBehaviour. Driven by external calls
    /// (from GameManager/NetworkController) and fires events via GameEvents.
    /// </summary>
    public class MatchManager
    {
        #region Constants

        private const int MaxRounds = 3;
        private const float PlanningTimeRound1 = 30f;
        private const float PlanningTimeLater = 20f;

        #endregion

        #region Properties

        public GamePhase CurrentPhase { get; private set; }
        public int CurrentRound { get; private set; }
        public HeroState Player1 { get; private set; }
        public HeroState Player2 { get; private set; }
        public GridSystem Grid { get; private set; }
        public MapConfig Map { get; private set; }

        public float CurrentPlanningTime => CurrentRound == 1 ? PlanningTimeRound1 : PlanningTimeLater;

        #endregion

        #region Fields

        private ActionResolver _resolver;
        private ShrinkSystem _shrink;
        private List<ActionType> _p1Actions;
        private List<ActionType> _p2Actions;
        private bool _p1Submitted;
        private bool _p2Submitted;
        private List<StepResult> _lastRoundResults;

        #endregion

        #region Match Lifecycle

        /// <summary>
        /// Initializes and starts a new match with given heroes and map.
        /// </summary>
        public void StartMatch(HeroConfig p1Hero, HeroConfig p2Hero, MapConfig map)
        {
            Map = map;
            Grid = new GridSystem(map);
            Player1 = new HeroState(p1Hero, 0, map.player1Spawn, map.player1Facing);
            Player2 = new HeroState(p2Hero, 1, map.player2Spawn, map.player2Facing);
            _resolver = new ActionResolver(Grid, Player1, Player2);
            _shrink = new ShrinkSystem(Grid, map);
            CurrentRound = 1;
            _lastRoundResults = new List<StepResult>(8);

            var startData = new MatchStartData
            {
                P1Hero = p1Hero,
                P2Hero = p2Hero,
                Map = map,
                P1Spawn = map.player1Spawn,
                P2Spawn = map.player2Spawn,
                P1Facing = map.player1Facing,
                P2Facing = map.player2Facing,
                MaxRounds = MaxRounds
            };

            GameEvents.MatchStarted(startData);
            StartPlanningPhase();
        }

        #endregion

        #region Planning Phase

        private void StartPlanningPhase()
        {
            CurrentPhase = GamePhase.Planning;
            _p1Actions = null;
            _p2Actions = null;
            _p1Submitted = false;
            _p2Submitted = false;

            GameEvents.PhaseChanged(GamePhase.Planning);
            GameEvents.RoundStarted(CurrentRound);
        }

        /// <summary>
        /// Receives a player's action list. When both submitted, starts execution.
        /// Returns null on success, or error string if validation fails.
        /// </summary>
        public string SubmitActions(int playerIndex, List<ActionType> actions)
        {
            if (CurrentPhase != GamePhase.Planning)
                return "Not in planning phase";

            var hero = playerIndex == 0 ? Player1.Config : Player2.Config;

            actions = ActionValidator.PadWithWait(actions, hero.steps);
            string error = ActionValidator.Validate(actions, hero);
            if (error != null)
                return error;

            if (playerIndex == 0)
            {
                _p1Actions = actions;
                _p1Submitted = true;
            }
            else
            {
                _p2Actions = actions;
                _p2Submitted = true;
            }

            if (_p1Submitted && _p2Submitted)
                ExecuteRound();

            return null;
        }

        /// <summary>
        /// Whether a specific player has already submitted their actions.
        /// </summary>
        public bool HasSubmitted(int playerIndex) => playerIndex == 0 ? _p1Submitted : _p2Submitted;

        #endregion

        #region Execution Phase

        private void ExecuteRound()
        {
            CurrentPhase = GamePhase.Execution;
            GameEvents.PhaseChanged(GamePhase.Execution);

            _lastRoundResults.Clear();
            int maxSteps = Mathf.Max(_p1Actions.Count, _p2Actions.Count);

            for (int step = 0; step < maxSteps; step++)
            {
                var p1Act = step < _p1Actions.Count ? _p1Actions[step] : ActionType.Wait;
                var p2Act = step < _p2Actions.Count ? _p2Actions[step] : ActionType.Wait;

                StepResult result = _resolver.ResolveStep(step, p1Act, p2Act);
                _lastRoundResults.Add(result);
                GameEvents.StepResolved(result);

                if (result.P1ArmorBroken)
                    GameEvents.ArmorChanged(0, false);
                if (result.P2ArmorBroken)
                    GameEvents.ArmorChanged(1, false);

                if (result.P1Eliminated || result.P2Eliminated)
                {
                    if (result.P1Eliminated)
                        GameEvents.HeroEliminated(0);
                    if (result.P2Eliminated)
                        GameEvents.HeroEliminated(1);

                    var roundResult = result.P1Eliminated
                        ? RoundResult.Player2Kill
                        : RoundResult.Player1Kill;
                    EndRound(roundResult);
                    return;
                }
            }

            // No kill: check if anyone is in danger zone at end of round
            bool p1InDanger = Grid.IsInDangerZone(Player1.Position) && Player1.IsAlive;
            bool p2InDanger = Grid.IsInDangerZone(Player2.Position) && Player2.IsAlive;

            if (p1InDanger && p2InDanger)
            {
                Player1.IsAlive = false;
                Player2.IsAlive = false;
                EndRound(RoundResult.MutualCancel);
                return;
            }

            if (p1InDanger)
            {
                Player1.IsAlive = false;
                GameEvents.HeroEliminated(0);
                EndRound(RoundResult.Player2Kill);
                return;
            }

            if (p2InDanger)
            {
                Player2.IsAlive = false;
                GameEvents.HeroEliminated(1);
                EndRound(RoundResult.Player1Kill);
                return;
            }

            if (CurrentRound >= MaxRounds)
                EndMatch(MatchResult.Draw);
            else
                EndRound(RoundResult.NoKill);
        }

        #endregion

        #region Round / Match End

        private void EndRound(RoundResult roundResult)
        {
            CurrentPhase = GamePhase.PostRound;
            GameEvents.PhaseChanged(GamePhase.PostRound);
            GameEvents.RoundEnded(roundResult);

            switch (roundResult)
            {
                case RoundResult.Player1Kill:
                    EndMatch(MatchResult.Player2Win);
                    break;
                case RoundResult.Player2Kill:
                    EndMatch(MatchResult.Player1Win);
                    break;
                case RoundResult.MutualCancel:
                    EndMatch(MatchResult.Draw);
                    break;
                case RoundResult.NoKill:
                    CurrentRound++;
                    _shrink.ApplyShrink(CurrentRound);
                    Player1.ResetForNewRound();
                    Player2.ResetForNewRound();
                    StartPlanningPhase();
                    break;
            }
        }

        private void EndMatch(MatchResult result)
        {
            CurrentPhase = GamePhase.PostMatch;
            GameEvents.PhaseChanged(GamePhase.PostMatch);
            GameEvents.MatchEnded(result);
        }

        #endregion

        #region Queries

        /// <summary>
        /// Returns the results of the last executed round for replay/display.
        /// </summary>
        public IReadOnlyList<StepResult> GetLastRoundResults() => _lastRoundResults;

        #endregion
    }
}
