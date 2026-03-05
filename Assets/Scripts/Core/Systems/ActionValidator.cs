using System.Collections.Generic;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Validates a player's action list before execution.
    /// Shared logic: used on client for preview and on server for authoritative check.
    /// </summary>
    public static class ActionValidator
    {
        #region Public Methods

        /// <summary>
        /// Validates that the action list is legal for the given hero.
        /// Returns null if valid, or an error message describing the problem.
        /// </summary>
        public static string Validate(List<ActionType> actions, HeroConfig hero)
        {
            if (actions == null)
                return "Actions list is null";

            if (actions.Count != hero.steps)
                return $"Expected {hero.steps} actions, got {actions.Count}";

            int cooldownCounter = 0;
            bool specialUsed = false;

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];

                switch (action)
                {
                    case ActionType.Shoot:
                        if (cooldownCounter > 0)
                            return $"Shoot at step {i} violates cooldown (remaining: {cooldownCounter})";
                        cooldownCounter = hero.cooldown;
                        break;

                    case ActionType.Special:
                        if (specialUsed)
                            return $"Special at step {i} — already used this round";
                        specialUsed = true;
                        break;

                    case ActionType.Move:
                    case ActionType.TurnLeft:
                    case ActionType.TurnRight:
                    case ActionType.TurnAround:
                    case ActionType.Wait:
                        break;

                    default:
                        return $"Unknown action type at step {i}: {action}";
                }

                if (action != ActionType.Shoot)
                    cooldownCounter = cooldownCounter > 0 ? cooldownCounter - 1 : 0;
            }

            return null;
        }

        /// <summary>
        /// Pads an action list to the required length with Wait actions.
        /// Used when timer expires and player hasn't filled all slots.
        /// </summary>
        public static List<ActionType> PadWithWait(List<ActionType> actions, int targetCount)
        {
            actions ??= new List<ActionType>(targetCount);

            while (actions.Count < targetCount)
                actions.Add(ActionType.Wait);

            return actions;
        }

        /// <summary>
        /// Checks if a specific action can be placed at a given slot index,
        /// considering cooldown state from previous actions in the sequence.
        /// Used by planning UI to gray out unavailable actions.
        /// </summary>
        public static bool CanPlaceAction(List<ActionType> currentQueue, int slotIndex, ActionType action, HeroConfig hero)
        {
            if (slotIndex < 0 || slotIndex >= hero.steps)
                return false;

            if (action == ActionType.Special)
            {
                for (int i = 0; i < currentQueue.Count && i < slotIndex; i++)
                {
                    if (currentQueue[i] == ActionType.Special)
                        return false;
                }
                return true;
            }

            if (action == ActionType.Shoot)
            {
                int cd = 0;
                for (int i = 0; i < slotIndex && i < currentQueue.Count; i++)
                {
                    if (currentQueue[i] == ActionType.Shoot)
                        cd = hero.cooldown;
                    else
                        cd = cd > 0 ? cd - 1 : 0;
                }
                return cd <= 0;
            }

            return true;
        }

        #endregion
    }
}
