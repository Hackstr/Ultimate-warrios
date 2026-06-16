using System.Collections.Generic;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Scripted bot for tutorial. Behavior changes per round
    /// to create teachable moments for the player.
    /// </summary>
    public static class TutorialBotAI
    {
        /// <summary>
        /// Returns predictable actions for tutorial based on the current round.
        /// </summary>
        public static List<ActionType> GetActions(int heroSteps, int round = 1)
        {
            var actions = new List<ActionType>(heroSteps);

            switch (round)
            {
                case 1:
                    // Move forward twice, then wait — easy frontal target
                    actions.Add(ActionType.Move);
                    actions.Add(ActionType.Move);
                    break;
                case 2:
                    // Move once then turn — teaches player to predict turns
                    actions.Add(ActionType.Move);
                    actions.Add(ActionType.TurnLeft);
                    break;
                case 3:
                    // Move sideways, wait — teaches positioning for special
                    actions.Add(ActionType.TurnRight);
                    actions.Add(ActionType.Move);
                    break;
                default:
                    actions.Add(ActionType.Move);
                    actions.Add(ActionType.Move);
                    break;
            }

            while (actions.Count < heroSteps)
                actions.Add(ActionType.Wait);

            return actions;
        }
    }
}
