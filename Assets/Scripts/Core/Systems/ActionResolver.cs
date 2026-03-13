using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Core deterministic step resolver. Receives two actions per step,
    /// resolves Movement → Combat → Damage in strict order.
    /// Must produce identical results on client and server (TypeScript port).
    /// </summary>
    public class ActionResolver
    {
        #region Fields

        private readonly GridSystem _grid;
        private readonly HeroState _p1;
        private readonly HeroState _p2;

        #endregion

        #region Constructor

        public ActionResolver(GridSystem grid, HeroState p1, HeroState p2)
        {
            _grid = grid;
            _p1 = p1;
            _p2 = p2;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resolves a single step of simultaneous action.
        /// Both players' actions execute at the same time within each phase.
        /// Order: Phase 1 Movement → Phase 2 Combat → Phase 3 Damage → Cooldown update.
        /// </summary>
        public StepResult ResolveStep(int stepIndex, ActionType p1Action, ActionType p2Action)
        {
            var result = new StepResult
            {
                StepIndex = stepIndex,
                P1Action = p1Action,
                P2Action = p2Action,
                P1StartPos = _p1.Position,
                P1StartFacing = _p1.Facing,
                P2StartPos = _p2.Position,
                P2StartFacing = _p2.Facing
            };

            ResolveMovement(p1Action, p2Action, result);
            ResolveCombat(p1Action, p2Action, result);
            ResolveDamage(result);
            UpdateCooldowns(p1Action, p2Action, result);
            UpdateCloaking();

            result.P1EndPos = _p1.Position;
            result.P1EndFacing = _p1.Facing;
            result.P2EndPos = _p2.Position;
            result.P2EndFacing = _p2.Facing;

            return result;
        }

        #endregion

        #region Phase 1: Movement

        private void ResolveMovement(ActionType p1Act, ActionType p2Act, StepResult result)
        {
            Vector2Int p1Target = _p1.Position;
            Vector2Int p2Target = _p2.Position;

            // Apply turns (instant, no collision possible)
            ApplyTurn(_p1, p1Act);
            ApplyTurn(_p2, p2Act);

            // Calculate move targets
            if (p1Act == ActionType.Move)
                p1Target = _grid.GetMoveTarget(_p1.Position, _p1.Facing, _p1.EffectiveSpeed);

            if (p2Act == ActionType.Move)
                p2Target = _grid.GetMoveTarget(_p2.Position, _p2.Facing, _p2.EffectiveSpeed);

            // Collision: both moving to the same tile — both stay put
            if (p1Target == p2Target && p1Target != _p1.Position && p2Target != _p2.Position)
            {
                p1Target = _p1.Position;
                p2Target = _p2.Position;
            }

            // Swap collision: P1 moving to P2's pos AND P2 moving to P1's pos
            if (p1Target == _p2.Position && p2Target == _p1.Position)
            {
                p1Target = _p1.Position;
                p2Target = _p2.Position;
            }

            // Moving into opponent's stationary position — stop before them
            if (p1Act == ActionType.Move && p2Act != ActionType.Move && p1Target == _p2.Position)
                p1Target = StopBeforeTarget(_p1.Position, _p1.Facing, _p1.EffectiveSpeed, _p2.Position);

            if (p2Act == ActionType.Move && p1Act != ActionType.Move && p2Target == _p1.Position)
                p2Target = StopBeforeTarget(_p2.Position, _p2.Facing, _p2.EffectiveSpeed, _p1.Position);

            _p1.Position = p1Target;
            _p2.Position = p2Target;

            // Pickup collection
            CheckPickup(_p1, result, isPlayer1: true);
            CheckPickup(_p2, result, isPlayer1: false);
        }

        private static void ApplyTurn(HeroState hero, ActionType action)
        {
            switch (action)
            {
                case ActionType.TurnLeft:
                    hero.Facing = GridSystem.TurnLeft(hero.Facing);
                    break;
                case ActionType.TurnRight:
                    hero.Facing = GridSystem.TurnRight(hero.Facing);
                    break;
                case ActionType.TurnAround:
                    hero.Facing = GridSystem.TurnAround(hero.Facing);
                    break;
            }
        }

        /// <summary>
        /// When moving with speed > 1 toward an occupied tile,
        /// stop at the last empty tile before the blocker.
        /// </summary>
        private Vector2Int StopBeforeTarget(Vector2Int from, Direction dir, int speed, Vector2Int blockerPos)
        {
            var step = GridSystem.DirectionToVector(dir);
            var current = from;

            for (int i = 0; i < speed; i++)
            {
                var next = current + step;
                if (next == blockerPos || !_grid.IsWalkable(next))
                    break;
                current = next;
            }

            return current;
        }

        private void CheckPickup(HeroState hero, StepResult result, bool isPlayer1)
        {
            if (!_grid.HasPickup(hero.Position))
                return;

            var pickupType = _grid.GetPickup(hero.Position);
            if (pickupType == PickupType.None)
                return;

            if (isPlayer1)
                result.P1PickedUp = pickupType;
            else
                result.P2PickedUp = pickupType;

            ApplyPickup(hero, pickupType);
            _grid.RemovePickup(hero.Position);
        }

        private static void ApplyPickup(HeroState hero, PickupType type)
        {
            switch (type)
            {
                case PickupType.ArmorShard:
                    hero.HasArmor = true;
                    break;
                case PickupType.IntelOrb:
                    hero.HasIntel = true;
                    break;
                case PickupType.SpeedBoost:
                    hero.BonusSpeed = 1;
                    break;
                case PickupType.RangeBoost:
                    hero.BonusRange = 2;
                    break;
            }
        }

        #endregion

        #region Phase 2: Combat

        private void ResolveCombat(ActionType p1Act, ActionType p2Act, StepResult result)
        {
            bool p1Shoots = p1Act == ActionType.Shoot && _p1.CooldownRemaining <= 0;
            bool p2Shoots = p2Act == ActionType.Shoot && _p2.CooldownRemaining <= 0;

            result.P1Fired = p1Shoots;
            result.P2Fired = p2Shoots;

            if (p1Shoots)
            {
                var rayTiles = _grid.CastRay(_p1.Position, _p1.Facing, _p1.EffectiveRange);
                result.P1Hit = TilesContain(rayTiles, _p2.Position);
            }

            if (p2Shoots)
            {
                var rayTiles = _grid.CastRay(_p2.Position, _p2.Facing, _p2.EffectiveRange);
                result.P2Hit = TilesContain(rayTiles, _p1.Position);
            }
        }

        /// <summary>
        /// Uses manual iteration instead of LINQ .Contains() to avoid GC allocation
        /// (CastRay returns a shared buffer, not a new list).
        /// </summary>
        private static bool TilesContain(List<Vector2Int> tiles, Vector2Int target)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == target)
                    return true;
            }
            return false;
        }

        #endregion

        #region Phase 3: Damage

        private void ResolveDamage(StepResult result)
        {
            // Mutual cancel: both shots hit — both nullified, nobody takes damage
            if (result.P1Hit && result.P2Hit)
            {
                result.MutualCancel = true;
                result.P1Hit = false;
                result.P2Hit = false;
                return;
            }

            if (result.P1Hit)
                ApplyDamage(_p2, result, isTarget1: false);

            if (result.P2Hit)
                ApplyDamage(_p1, result, isTarget1: true);
        }

        private static void ApplyDamage(HeroState target, StepResult result, bool isTarget1)
        {
            if (target.HasArmor)
            {
                target.HasArmor = false;
                if (isTarget1)
                    result.P1ArmorBroken = true;
                else
                    result.P2ArmorBroken = true;
            }
            else
            {
                target.IsAlive = false;
                if (isTarget1)
                    result.P1Eliminated = true;
                else
                    result.P2Eliminated = true;
            }
        }

        #endregion

        #region Cooldown & State Updates

        private void UpdateCooldowns(ActionType p1Act, ActionType p2Act, StepResult result)
        {
            // Set cooldown if player actually fired this step
            if (result.P1Fired)
                _p1.CooldownRemaining = _p1.Config.cooldown;
            if (result.P2Fired)
                _p2.CooldownRemaining = _p2.Config.cooldown;

            // Tick down cooldowns (happens every step regardless)
            _p1.CooldownRemaining = Mathf.Max(0, _p1.CooldownRemaining - 1);
            _p2.CooldownRemaining = Mathf.Max(0, _p2.CooldownRemaining - 1);
        }

        private void UpdateCloaking()
        {
            TickCloak(_p1);
            TickCloak(_p2);
        }

        private static void TickCloak(HeroState hero)
        {
            if (!hero.IsCloaked)
                return;

            hero.CloakStepsRemaining--;
            if (hero.CloakStepsRemaining <= 0)
            {
                hero.IsCloaked = false;
                hero.CloakStepsRemaining = 0;
            }
        }

        #endregion
    }
}
