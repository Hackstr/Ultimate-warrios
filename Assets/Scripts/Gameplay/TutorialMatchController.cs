using System.Collections.Generic;
using TacticalDuelist.Core.Localization;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Orchestrates an interactive 3-round tutorial match.
    /// Each round has a script of TutorialSteps that guide the player
    /// through specific actions with highlighted buttons.
    /// </summary>
    public class TutorialMatchController
    {
        private readonly List<TutorialStep>[] _roundScripts;
        private int _currentRound;
        private int _currentStepIndex;

        public TutorialStep CurrentStep
        {
            get
            {
                var script = GetCurrentScript();
                if (script == null || _currentStepIndex >= script.Count) return null;
                return script[_currentStepIndex];
            }
        }

        public bool IsRoundScriptComplete
        {
            get
            {
                var script = GetCurrentScript();
                return script == null || _currentStepIndex >= script.Count;
            }
        }

        public TutorialMatchController()
        {
            _roundScripts = new[]
            {
                // Round 1: Move + Shoot basics
                new List<TutorialStep>
                {
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Move,
                            HintText = L.Get("tut_tap_move"), ButtonId = "btn-move" },
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Move,
                            HintText = L.Get("tut_move_again"), ButtonId = "btn-move" },
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Shoot,
                            HintText = L.Get("tut_tap_shoot"), ButtonId = "btn-shoot" },
                },
                // Round 2: Turn + positioning
                new List<TutorialStep>
                {
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.TurnRight,
                            HintText = L.Get("tut_tap_turn"), ButtonId = "btn-turn-right" },
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Move,
                            HintText = L.Get("tut_reposition"), ButtonId = "btn-move" },
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Shoot,
                            HintText = L.Get("tut_shoot_enemy"), ButtonId = "btn-shoot" },
                },
                // Round 3: Special ability
                new List<TutorialStep>
                {
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Move,
                            HintText = L.Get("tut_get_position"), ButtonId = "btn-move" },
                    new() { Type = TutorialStepType.TapAction, RequiredAction = ActionType.Special,
                            HintText = L.Get("tut_use_special"), ButtonId = "btn-special" },
                },
            };
        }

        public void SetRound(int round)
        {
            _currentRound = round - 1; // 0-indexed
            _currentStepIndex = 0;
        }

        /// <summary>
        /// Called when player adds an action. Advances if it matches the expected action.
        /// Returns true if the step was advanced.
        /// </summary>
        public bool OnActionAdded(ActionType action)
        {
            var step = CurrentStep;
            if (step == null) return false;
            if (step.Type != TutorialStepType.TapAction) return false;
            if (step.RequiredAction != action) return false;

            _currentStepIndex++;
            return true;
        }

        public string GetPostRoundFeedback(RoundResult result)
        {
            int round = _currentRound + 1;
            return round switch
            {
                1 => result == RoundResult.Player1Kill
                    ? L.Get("tut_great_shot")
                    : L.Get("tut_good_try"),
                2 => L.Get("tut_turn_tip"),
                3 => L.Get("tut_special_tip"),
                _ => L.Get("tut_well_played")
            };
        }

        private List<TutorialStep> GetCurrentScript()
        {
            if (_currentRound < 0 || _currentRound >= _roundScripts.Length) return null;
            return _roundScripts[_currentRound];
        }
    }
}
