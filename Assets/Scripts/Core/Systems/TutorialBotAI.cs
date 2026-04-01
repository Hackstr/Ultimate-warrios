using System.Collections.Generic;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Scripted bot for tutorial. Moves predictably so the player can learn
    /// mechanics and win their first match.
    /// </summary>
    public static class TutorialBotAI
    {
        /// <summary>
        /// Returns predictable actions for tutorial.
        /// Bot moves forward twice then waits — easy target for the player.
        /// </summary>
        public static List<ActionType> GetActions(int heroSteps)
        {
            var actions = new List<ActionType>(heroSteps);

            // Move forward twice, then wait for remaining steps
            actions.Add(ActionType.Move);
            actions.Add(ActionType.Move);

            for (int i = 2; i < heroSteps; i++)
                actions.Add(ActionType.Wait);

            return actions;
        }
    }
}
