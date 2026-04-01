using UnityEngine;

namespace TacticalDuelist.Core.Models
{
    /// <summary>
    /// Immutable output of a single step resolution. Consumed by View layer for animation.
    /// </summary>
    public class StepResult
    {
        #region Step Info

        public int StepIndex;

        #endregion

        #region Player 1 State

        public ActionType P1Action;
        public Vector2Int P1StartPos;
        public Vector2Int P1EndPos;
        public Direction P1StartFacing;
        public Direction P1EndFacing;

        #endregion

        #region Player 2 State

        public ActionType P2Action;
        public Vector2Int P2StartPos;
        public Vector2Int P2EndPos;
        public Direction P2StartFacing;
        public Direction P2EndFacing;

        #endregion

        #region Combat Results

        public bool P1Fired;
        public bool P2Fired;
        public bool P1Hit;
        public bool P2Hit;
        public bool MutualCancel;
        public bool P1ArmorBroken;
        public bool P2ArmorBroken;
        public bool P1Eliminated;
        public bool P2Eliminated;
        public bool P1Shielded;
        public bool P2Shielded;

        #endregion

        #region Special & Pickups

        public SpecialResult P1Special;
        public SpecialResult P2Special;
        public PickupType P1PickedUp = PickupType.None;
        public PickupType P2PickedUp = PickupType.None;

        #endregion
    }
}
