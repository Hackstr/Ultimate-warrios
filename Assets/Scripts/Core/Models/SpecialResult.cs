using UnityEngine;

namespace TacticalDuelist.Core.Models
{
    /// <summary>
    /// Data about a special ability activation during a step.
    /// Attached to StepResult when a player uses their special.
    /// </summary>
    public class SpecialResult
    {
        public SpecialAbility Ability;
        public Vector2Int? TargetPosition;
        public Direction? TargetDirection;
    }
}
