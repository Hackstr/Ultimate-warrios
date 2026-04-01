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

            // Shadow Blink: teleport forward through walls
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Blink)
            {
                p1Target = ResolveBlink(_p1);
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Blink, HasTargetPosition = true, TargetPosition = p1Target };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Blink)
            {
                p2Target = ResolveBlink(_p2);
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Blink, HasTargetPosition = true, TargetPosition = p2Target };
            }

            // Berserker Charge: dash 3 tiles forward, hitting opponent along the way
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Charge)
            {
                p1Target = ResolveCharge(_p1, _p2);
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Charge, HasTargetPosition = true, TargetPosition = p1Target };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Charge)
            {
                p2Target = ResolveCharge(_p2, _p1);
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Charge, HasTargetPosition = true, TargetPosition = p2Target };
            }

            // Guardian Barrier: place temporary wall in front of hero
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Barrier)
            {
                ResolveBarrier(_p1);
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Barrier, HasTargetPosition = true, TargetPosition = _p1.BarrierPosition };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Barrier)
            {
                ResolveBarrier(_p2);
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Barrier, HasTargetPosition = true, TargetPosition = _p2.BarrierPosition };
            }

            // Demo Bomb: place bomb at current position, damages adjacent tiles immediately
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Bomb)
            {
                ResolveBombPlace(_p1);
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Bomb, HasTargetPosition = true, TargetPosition = _p1.BombPosition };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Bomb)
            {
                ResolveBombPlace(_p2);
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Bomb, HasTargetPosition = true, TargetPosition = _p2.BombPosition };
            }

            // Engineer Turret: place turret at current position facing hero's direction
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Turret)
            {
                ResolveTurretPlace(_p1);
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Turret, HasTargetPosition = true, TargetPosition = _p1.TurretPosition, HasTargetDirection = true, TargetDirection = _p1.TurretFacing };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Turret)
            {
                ResolveTurretPlace(_p2);
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Turret, HasTargetPosition = true, TargetPosition = _p2.TurretPosition, HasTargetDirection = true, TargetDirection = _p2.TurretFacing };
            }

            // Ghost Cloak: become invisible for 3 steps
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Cloak)
            {
                _p1.IsCloaked = true;
                _p1.CloakStepsRemaining = 3;
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Cloak };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Cloak)
            {
                _p2.IsCloaked = true;
                _p2.CloakStepsRemaining = 3;
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Cloak };
            }

            // Mirage Decoy: place decoy at current position, move hero 1 tile forward
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Decoy)
            {
                p1Target = ResolveDecoy(_p1, _p2.Position);
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Decoy, HasTargetPosition = true, TargetPosition = _p1.DecoyPosition };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Decoy)
            {
                p2Target = ResolveDecoy(_p2, _p1.Position);
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Decoy, HasTargetPosition = true, TargetPosition = _p2.DecoyPosition };
            }

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
            // Archer Ricochet: Special action fires with ricochet ray
            bool p1Ricochet = p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Ricochet;
            bool p2Ricochet = p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Ricochet;

            // Tank Push: Special action fires a normal shot (push applied in ResolveDamage if hit)
            bool p1Push = p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Push;
            bool p2Push = p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Push;

            // Mage PhaseShot: shot passes through 1 wall
            bool p1PhaseShot = p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.PhaseShot;
            bool p2PhaseShot = p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.PhaseShot;

            // Hawk Pierce: shot passes through ALL walls
            bool p1Pierce = p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Pierce;
            bool p2Pierce = p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Pierce;

            bool p1Shoots = (p1Act == ActionType.Shoot || p1Ricochet || p1Push || p1PhaseShot || p1Pierce) && _p1.CooldownRemaining <= 0;
            bool p2Shoots = (p2Act == ActionType.Shoot || p2Ricochet || p2Push || p2PhaseShot || p2Pierce) && _p2.CooldownRemaining <= 0;

            result.P1Fired = p1Shoots;
            result.P2Fired = p2Shoots;

            // Determine P2's effective position for P1's shot (Decoy intercept)
            Vector2Int p2TargetPos = _p2.Position;
            Vector2Int p1TargetPos = _p1.Position;

            if (p1Shoots)
            {
                List<Vector2Int> rayTiles;
                if (p1Ricochet)
                {
                    rayTiles = _grid.CastRayRicochet(_p1.Position, _p1.Facing, _p1.EffectiveRange);
                    _p1.SpecialUsedThisRound = true;
                    var lastTile = rayTiles.Count > 0 ? rayTiles[rayTiles.Count - 1] : _p1.Position;
                    result.P1Special = new SpecialResult { Ability = SpecialAbility.Ricochet, HasTargetPosition = true, TargetPosition = lastTile };
                }
                else if (p1PhaseShot)
                {
                    rayTiles = _grid.CastRayPhase(_p1.Position, _p1.Facing, _p1.EffectiveRange);
                    _p1.SpecialUsedThisRound = true;
                    var lastTile = rayTiles.Count > 0 ? rayTiles[rayTiles.Count - 1] : _p1.Position;
                    result.P1Special = new SpecialResult { Ability = SpecialAbility.PhaseShot, HasTargetPosition = true, TargetPosition = lastTile };
                }
                else if (p1Pierce)
                {
                    rayTiles = _grid.CastRayPierce(_p1.Position, _p1.Facing, _p1.EffectiveRange);
                    _p1.SpecialUsedThisRound = true;
                    var lastTile = rayTiles.Count > 0 ? rayTiles[rayTiles.Count - 1] : _p1.Position;
                    result.P1Special = new SpecialResult { Ability = SpecialAbility.Pierce, HasTargetPosition = true, TargetPosition = lastTile };
                }
                else
                {
                    rayTiles = _grid.CastRay(_p1.Position, _p1.Facing, _p1.EffectiveRange);
                }

                // Check if shot hits opponent's decoy first (absorbs the shot)
                if (_p2.DecoyActive && TilesContainBefore(rayTiles, _p2.DecoyPosition, p2TargetPos))
                {
                    _p2.DecoyActive = false;
                    result.P1Hit = false;
                }
                else
                {
                    result.P1Hit = TilesContain(rayTiles, p2TargetPos);
                    // If shot misses the real hero but hits the decoy, destroy the decoy
                    if (!result.P1Hit && _p2.DecoyActive && TilesContain(rayTiles, _p2.DecoyPosition))
                        _p2.DecoyActive = false;
                }
            }

            if (p2Shoots)
            {
                List<Vector2Int> rayTiles;
                if (p2Ricochet)
                {
                    rayTiles = _grid.CastRayRicochet(_p2.Position, _p2.Facing, _p2.EffectiveRange);
                    _p2.SpecialUsedThisRound = true;
                    var lastTile = rayTiles.Count > 0 ? rayTiles[rayTiles.Count - 1] : _p2.Position;
                    result.P2Special = new SpecialResult { Ability = SpecialAbility.Ricochet, HasTargetPosition = true, TargetPosition = lastTile };
                }
                else if (p2PhaseShot)
                {
                    rayTiles = _grid.CastRayPhase(_p2.Position, _p2.Facing, _p2.EffectiveRange);
                    _p2.SpecialUsedThisRound = true;
                    var lastTile = rayTiles.Count > 0 ? rayTiles[rayTiles.Count - 1] : _p2.Position;
                    result.P2Special = new SpecialResult { Ability = SpecialAbility.PhaseShot, HasTargetPosition = true, TargetPosition = lastTile };
                }
                else if (p2Pierce)
                {
                    rayTiles = _grid.CastRayPierce(_p2.Position, _p2.Facing, _p2.EffectiveRange);
                    _p2.SpecialUsedThisRound = true;
                    var lastTile = rayTiles.Count > 0 ? rayTiles[rayTiles.Count - 1] : _p2.Position;
                    result.P2Special = new SpecialResult { Ability = SpecialAbility.Pierce, HasTargetPosition = true, TargetPosition = lastTile };
                }
                else
                {
                    rayTiles = _grid.CastRay(_p2.Position, _p2.Facing, _p2.EffectiveRange);
                }

                // Check if shot hits opponent's decoy first (absorbs the shot)
                if (_p1.DecoyActive && TilesContainBefore(rayTiles, _p1.DecoyPosition, p1TargetPos))
                {
                    _p1.DecoyActive = false;
                    result.P2Hit = false;
                }
                else
                {
                    result.P2Hit = TilesContain(rayTiles, p1TargetPos);
                    // If shot misses the real hero but hits the decoy, destroy the decoy
                    if (!result.P2Hit && _p1.DecoyActive && TilesContain(rayTiles, _p1.DecoyPosition))
                        _p1.DecoyActive = false;
                }
            }

            // Engineer Turret auto-fire (fires once the step it's placed, then destroyed)
            ResolveTurretFire(_p1, _p2, result, isP1Turret: true);
            ResolveTurretFire(_p2, _p1, result, isP1Turret: false);

            // Berserker Charge hit (resolved during movement, applied as combat hit)
            if (_p1.ChargeHit)
            {
                result.P1Hit = true;
                _p1.ChargeHit = false;
            }
            if (_p2.ChargeHit)
            {
                result.P2Hit = true;
                _p2.ChargeHit = false;
            }

            // Demo Bomb detonation: damage anyone on adjacent tiles (cross pattern)
            ResolveBombDetonation(_p1, _p2, result, isP1Bomb: true);
            ResolveBombDetonation(_p2, _p1, result, isP1Bomb: false);
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

        /// <summary>
        /// Checks if the ray hits posA before posB. Used for decoy interception:
        /// if the decoy is closer along the ray path than the real hero, the decoy absorbs the shot.
        /// </summary>
        private static bool TilesContainBefore(List<Vector2Int> tiles, Vector2Int posA, Vector2Int posB)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == posA)
                    return true;
                if (tiles[i] == posB)
                    return false;
            }
            return false;
        }

        #endregion

        #region Phase 3: Damage

        private void ResolveDamage(StepResult result)
        {
            // Shield action: grant temporary armor for this step
            if (result.P1Action == ActionType.Shield && !_p1.HasArmor)
            {
                _p1.HasArmor = true;
                result.P1Shielded = true;
            }
            if (result.P2Action == ActionType.Shield && !_p2.HasArmor)
            {
                _p2.HasArmor = true;
                result.P2Shielded = true;
            }

            // Mutual cancel: both shots hit — both nullified, nobody takes damage
            if (result.P1Hit && result.P2Hit)
            {
                result.MutualCancel = true;
                result.P1Hit = false;
                result.P2Hit = false;
                // Remove shield-granted armor if no hit absorbed it
                if (result.P1Shielded) _p1.HasArmor = false;
                if (result.P2Shielded) _p2.HasArmor = false;
                return;
            }

            if (result.P1Hit)
            {
                ApplyDamage(_p2, result, isTarget1: false);
                // Tank Push: push enemy 1 tile in Tank's facing direction
                if (_p1.Config.specialAbility == SpecialAbility.Push && result.P1Action == ActionType.Special)
                {
                    ResolvePush(_p1, _p2);
                    _p1.SpecialUsedThisRound = true;
                    result.P1Special = new SpecialResult { Ability = SpecialAbility.Push, HasTargetPosition = true, TargetPosition = _p2.Position };
                }
            }

            if (result.P2Hit)
            {
                ApplyDamage(_p1, result, isTarget1: true);
                // Tank Push: push enemy 1 tile in Tank's facing direction
                if (_p2.Config.specialAbility == SpecialAbility.Push && result.P2Action == ActionType.Special)
                {
                    ResolvePush(_p2, _p1);
                    _p2.SpecialUsedThisRound = true;
                    result.P2Special = new SpecialResult { Ability = SpecialAbility.Push, HasTargetPosition = true, TargetPosition = _p1.Position };
                }
            }

            // Remove shield-granted armor after damage resolution (temporary, one-step only)
            if (result.P1Shielded && !result.P1ArmorBroken)
                _p1.HasArmor = false;
            if (result.P2Shielded && !result.P2ArmorBroken)
                _p2.HasArmor = false;
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

            // Scout Scan: reveal opponent intel
            if (p1Act == ActionType.Special && _p1.Config.specialAbility == SpecialAbility.Scan)
            {
                _p2.HasIntel = true;
                _p1.SpecialUsedThisRound = true;
                result.P1Special = new SpecialResult { Ability = SpecialAbility.Scan };
            }

            if (p2Act == ActionType.Special && _p2.Config.specialAbility == SpecialAbility.Scan)
            {
                _p1.HasIntel = true;
                _p2.SpecialUsedThisRound = true;
                result.P2Special = new SpecialResult { Ability = SpecialAbility.Scan };
            }
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

        #region Special Ability Helpers

        /// <summary>
        /// Shadow Blink: teleport 2 tiles forward, ignoring walls.
        /// If the target tile (2 ahead) is not walkable, try 1 tile forward.
        /// If that's also not walkable, stay in place.
        /// </summary>
        private Vector2Int ResolveBlink(HeroState hero)
        {
            var step = GridSystem.DirectionToVector(hero.Facing);
            var target2 = hero.Position + step * 2;
            if (_grid.IsInBounds(target2) && _grid.IsWalkable(target2))
                return target2;

            var target1 = hero.Position + step;
            if (_grid.IsInBounds(target1) && _grid.IsWalkable(target1))
                return target1;

            return hero.Position;
        }

        /// <summary>
        /// Tank Push: push the target 1 tile in the attacker's facing direction.
        /// Target stays in place if the destination is not walkable or occupied.
        /// </summary>
        private void ResolvePush(HeroState attacker, HeroState target)
        {
            var pushDir = GridSystem.DirectionToVector(attacker.Facing);
            var pushTarget = target.Position + pushDir;

            if (_grid.IsWalkable(pushTarget) && pushTarget != attacker.Position)
                target.Position = pushTarget;
        }

        /// <summary>
        /// Berserker Charge: dash up to 3 tiles forward, stopping at walls.
        /// If any tile along the path contains the opponent, mark ChargeHit.
        /// </summary>
        private Vector2Int ResolveCharge(HeroState charger, HeroState opponent)
        {
            var step = GridSystem.DirectionToVector(charger.Facing);
            var current = charger.Position;

            for (int i = 0; i < 3; i++)
            {
                var next = current + step;

                if (!_grid.IsWalkable(next))
                    break;

                if (next == opponent.Position)
                {
                    charger.ChargeHit = true;
                    // Stop one tile before the opponent
                    break;
                }

                current = next;
            }

            return current;
        }

        /// <summary>
        /// Guardian Barrier: place a temporary wall on the tile directly in front.
        /// Removes any previously placed barrier first.
        /// </summary>
        private void ResolveBarrier(HeroState hero)
        {
            // Remove existing barrier if any
            if (hero.BarrierActive)
            {
                _grid.RemoveBarrier(hero.BarrierPosition);
                hero.BarrierActive = false;
            }

            var targetPos = hero.Position + GridSystem.DirectionToVector(hero.Facing);

            if (_grid.IsInBounds(targetPos) && _grid.IsWalkable(targetPos))
            {
                _grid.PlaceBarrier(targetPos);
                hero.BarrierPosition = targetPos;
                hero.BarrierActive = true;
            }
        }

        /// <summary>
        /// Demo Bomb: places a bomb at the hero's current position.
        /// </summary>
        private static void ResolveBombPlace(HeroState hero)
        {
            hero.BombPosition = hero.Position;
            hero.BombActive = true;
        }

        /// <summary>
        /// Demo Bomb detonation: damages opponent if they are on any of the 4 adjacent tiles
        /// (cross pattern: up/down/left/right of bomb, or on the bomb tile itself).
        /// Bomb is consumed after detonation check.
        /// </summary>
        private static void ResolveBombDetonation(HeroState bomber, HeroState opponent, StepResult result, bool isP1Bomb)
        {
            if (!bomber.BombActive)
                return;

            var bombPos = bomber.BombPosition;
            var oppPos = opponent.Position;

            // Check bomb tile itself + 4 cardinal adjacent tiles
            bool hit = oppPos == bombPos
                || oppPos == bombPos + Vector2Int.up
                || oppPos == bombPos + Vector2Int.down
                || oppPos == bombPos + Vector2Int.left
                || oppPos == bombPos + Vector2Int.right;

            if (hit)
            {
                if (isP1Bomb)
                    result.P1Hit = true;
                else
                    result.P2Hit = true;
            }

            // Bomb is consumed after detonation
            bomber.BombActive = false;
        }

        /// <summary>
        /// Engineer Turret: places a turret at current position facing hero's direction.
        /// </summary>
        private static void ResolveTurretPlace(HeroState hero)
        {
            hero.TurretPosition = hero.Position;
            hero.TurretFacing = hero.Facing;
            hero.TurretActive = true;
        }

        /// <summary>
        /// Engineer Turret auto-fire: fires a ray from turret position/facing (range 3).
        /// For MVP, turret fires once the step it's placed, then is destroyed.
        /// </summary>
        private void ResolveTurretFire(HeroState owner, HeroState opponent, StepResult result, bool isP1Turret)
        {
            if (!owner.TurretActive)
                return;

            var turretRay = _grid.CastRay(owner.TurretPosition, owner.TurretFacing, 3);
            bool turretHit = TilesContain(turretRay, opponent.Position);

            if (turretHit)
            {
                if (isP1Turret)
                    result.P1Hit = true;
                else
                    result.P2Hit = true;
            }

            // Turret is destroyed after firing (MVP simplification)
            owner.TurretActive = false;
        }

        /// <summary>
        /// Mirage Decoy: places decoy at current position, moves hero 1 tile forward.
        /// If the tile ahead is blocked or occupied, hero stays but decoy is still placed.
        /// </summary>
        private Vector2Int ResolveDecoy(HeroState hero, Vector2Int opponentPos)
        {
            hero.DecoyPosition = hero.Position;
            hero.DecoyActive = true;

            var step = GridSystem.DirectionToVector(hero.Facing);
            var forward = hero.Position + step;

            if (_grid.IsWalkable(forward) && forward != opponentPos)
                return forward;

            return hero.Position;
        }

        #endregion
    }
}
