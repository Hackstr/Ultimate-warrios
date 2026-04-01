using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Smart AI opponent. Evaluates board state, predicts opponent movement,
    /// and generates optimal action sequences.
    ///
    /// Strategy layers:
    /// 1. Threat assessment — am I about to get shot?
    /// 2. Opportunity detection — can I shoot the opponent?
    /// 3. Positioning — move toward a firing position
    /// 4. Special ability usage — use when it gives clear advantage
    /// </summary>
    public class BotAI
    {
        private readonly GridSystem _grid;
        private readonly HeroState _me;
        private readonly HeroState _opponent;

        public BotAI(GridSystem grid, HeroState me, HeroState opponent)
        {
            _grid = grid;
            _me = me;
            _opponent = opponent;
        }

        /// <summary>
        /// Generate the best action sequence for this turn.
        /// </summary>
        public List<ActionType> GenerateActions()
        {
            int steps = _me.Config.steps;
            var actions = new List<ActionType>(steps);

            // Simulate a copy of state for planning
            var simMe = CloneState(_me);
            var simOpp = CloneState(_opponent);

            for (int i = 0; i < steps; i++)
            {
                var action = ChooseBestAction(simMe, simOpp, steps - i);
                actions.Add(action);
                ApplyAction(simMe, action);
            }

            return actions;
        }

        private ActionType ChooseBestAction(SimState me, SimState opp, int stepsLeft)
        {
            // --- Priority 1: Shoot if we have line of sight ---
            if (me.CooldownRemaining <= 0 && HasLOS(me.Position, me.Facing, _me.Config.range))
            {
                // Check if opponent is in our firing line
                var shotTarget = GetShotEndpoint(me.Position, me.Facing, _me.Config.range);
                if (IsOpponentInFiringLine(me.Position, me.Facing, _me.Config.range, opp.Position))
                    return ActionType.Shoot;
            }

            // --- Priority 2: Use Special ability when advantageous ---
            if (stepsLeft >= 2 && !me.SpecialUsed && me.CooldownRemaining <= 0 && ShouldUseSpecial(me, opp))
                return ActionType.Special;

            // --- Priority 3: Turn to face opponent if close ---
            Direction desiredFacing = GetDirectionToward(me.Position, opp.Position);
            if (me.Facing != desiredFacing)
            {
                // Can we turn to face in one move?
                var turnAction = GetTurnAction(me.Facing, desiredFacing);
                if (turnAction != ActionType.Wait)
                {
                    // If we're already in range, turn to shoot
                    int dist = ManhattanDistance(me.Position, opp.Position);
                    if (dist <= _me.Config.range + 2)
                        return turnAction;
                }
            }

            // --- Priority 4: Move toward a good shooting position ---
            Vector2Int moveTarget = FindBestPosition(me, opp);
            if (moveTarget != me.Position)
            {
                // Need to face the movement direction first?
                Direction moveDir = GetDirectionToward(me.Position, moveTarget);
                if (me.Facing == moveDir)
                {
                    // Check if we can actually move forward
                    var nextPos = me.Position + GridSystem.DirectionToVector(me.Facing);
                    if (_grid.IsInBounds(nextPos) && _grid.IsWalkable(nextPos) && !_grid.IsInDangerZone(nextPos))
                        return ActionType.Move;
                }

                // Turn toward the target
                var turn = GetTurnAction(me.Facing, moveDir);
                if (turn != ActionType.Wait)
                    return turn;
            }

            // --- Priority 5: If facing opponent and on cooldown, wait ---
            if (me.CooldownRemaining > 0 && me.Facing == desiredFacing)
                return ActionType.Wait;

            // --- Priority 6: Dodge — move to avoid being in opponent's firing line ---
            if (IsInOpponentFiringLine(me.Position, opp))
            {
                var dodgeAction = GetDodgeAction(me, opp);
                if (dodgeAction != ActionType.Wait)
                    return dodgeAction;
            }

            // --- Fallback: Move forward or wait ---
            var fwdPos = me.Position + GridSystem.DirectionToVector(me.Facing);
            if (_grid.IsInBounds(fwdPos) && _grid.IsWalkable(fwdPos) && !_grid.IsInDangerZone(fwdPos))
                return ActionType.Move;

            return ActionType.Wait;
        }

        #region Tactical Analysis

        private bool IsOpponentInFiringLine(Vector2Int from, Direction facing, int range, Vector2Int oppPos)
        {
            var dir = GridSystem.DirectionToVector(facing);
            var pos = from;
            for (int i = 0; i < range; i++)
            {
                pos += dir;
                if (!_grid.IsInBounds(pos)) break;
                if (pos == oppPos) return true;
                if (_grid.GetTile(pos) == TileType.Wall) break;
            }
            return false;
        }

        private bool IsInOpponentFiringLine(Vector2Int myPos, SimState opp)
        {
            var dir = GridSystem.DirectionToVector(opp.Facing);
            var pos = opp.Position;
            for (int i = 0; i < 10; i++) // generous range check
            {
                pos += dir;
                if (!_grid.IsInBounds(pos)) break;
                if (pos == myPos) return true;
                if (_grid.GetTile(pos) == TileType.Wall) break;
            }
            return false;
        }

        private Vector2Int FindBestPosition(SimState me, SimState opp)
        {
            // Evaluate positions around me — find one where I can shoot the opponent
            int bestScore = int.MinValue;
            Vector2Int bestPos = me.Position;

            // Check positions within 2 tiles
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                var candidate = me.Position + new Vector2Int(dx, dy);
                if (!_grid.IsInBounds(candidate) || !_grid.IsWalkable(candidate))
                    continue;
                if (_grid.IsInDangerZone(candidate))
                    continue;

                int score = EvaluatePosition(candidate, me.Facing, opp);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPos = candidate;
                }
            }

            return bestPos;
        }

        private int EvaluatePosition(Vector2Int pos, Direction facing, SimState opp)
        {
            int score = 0;
            int dist = ManhattanDistance(pos, opp.Position);

            // Prefer being within shooting range
            if (dist <= _me.Config.range)
                score += 50;
            else
                score -= dist * 5;

            // Bonus for having LOS to opponent from any direction
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                if (IsOpponentInFiringLine(pos, dir, _me.Config.range, opp.Position))
                {
                    score += 30;
                    // Extra bonus if we're already facing that direction
                    if (dir == facing) score += 20;
                    break;
                }
            }

            // Penalty for being in opponent's firing line
            if (IsInOpponentFiringLine(pos, opp))
                score -= 40;

            // Prefer staying away from danger zones
            if (_grid.IsInDangerZone(pos))
                score -= 100;

            // Prefer distance of 3-5 tiles (safe but within range)
            if (dist >= 3 && dist <= 5)
                score += 15;

            return score;
        }

        private bool ShouldUseSpecial(SimState me, SimState opp)
        {
            var ability = _me.Config.specialAbility;
            int dist = ManhattanDistance(me.Position, opp.Position);

            switch (ability)
            {
                case SpecialAbility.Charge:
                    // Charge when facing opponent and within 3 tiles
                    return me.Facing == GetDirectionToward(me.Position, opp.Position) && dist <= 4;

                case SpecialAbility.Blink:
                    // Blink when we need to reposition quickly
                    return dist > _me.Config.range || IsInOpponentFiringLine(me.Position, opp);

                case SpecialAbility.Bomb:
                    // Bomb when close to opponent
                    return dist <= 2;

                case SpecialAbility.Barrier:
                    // Barrier when opponent is facing us and in range
                    return IsInOpponentFiringLine(me.Position, opp) && dist <= 6;

                case SpecialAbility.Cloak:
                    // Cloak when approaching opponent
                    return dist <= 5 && dist >= 2;

                case SpecialAbility.Turret:
                    // Turret when we have LOS to a chokepoint
                    return dist <= _me.Config.range;

                case SpecialAbility.Scan:
                    // Scan when opponent is cloaked or far away
                    return dist > 4;

                case SpecialAbility.Decoy:
                    // Decoy when opponent is close and facing us
                    return IsInOpponentFiringLine(me.Position, opp) && dist <= 4;

                default:
                    // Pierce, Ricochet, PhaseShot — use when we have LOS
                    return IsOpponentInFiringLine(me.Position, me.Facing, _me.Config.range + 2, opp.Position);
            }
        }

        private ActionType GetDodgeAction(SimState me, SimState opp)
        {
            // Try moving perpendicular to opponent's facing
            var oppDir = GridSystem.DirectionToVector(opp.Facing);
            // Perpendicular directions
            var perp1 = new Vector2Int(-oppDir.y, oppDir.x);
            var perp2 = new Vector2Int(oppDir.y, -oppDir.x);

            var pos1 = me.Position + perp1;
            var pos2 = me.Position + perp2;

            // Try to move to a safe perpendicular position
            Direction dir1 = VectorToDirection(perp1);
            Direction dir2 = VectorToDirection(perp2);

            if (me.Facing == dir1 && _grid.IsInBounds(pos1) && _grid.IsWalkable(pos1))
                return ActionType.Move;
            if (me.Facing == dir2 && _grid.IsInBounds(pos2) && _grid.IsWalkable(pos2))
                return ActionType.Move;

            // Need to turn first
            if (_grid.IsInBounds(pos1) && _grid.IsWalkable(pos1))
                return GetTurnAction(me.Facing, dir1);
            if (_grid.IsInBounds(pos2) && _grid.IsWalkable(pos2))
                return GetTurnAction(me.Facing, dir2);

            return ActionType.Wait;
        }

        #endregion

        #region Helpers

        private bool HasLOS(Vector2Int from, Direction facing, int range)
        {
            var dir = GridSystem.DirectionToVector(facing);
            var pos = from;
            for (int i = 0; i < range; i++)
            {
                pos += dir;
                if (!_grid.IsInBounds(pos)) return false;
                if (_grid.GetTile(pos) == TileType.Wall) return false;
            }
            return true;
        }

        private Vector2Int GetShotEndpoint(Vector2Int from, Direction facing, int range)
        {
            var dir = GridSystem.DirectionToVector(facing);
            var pos = from;
            for (int i = 0; i < range; i++)
            {
                var next = pos + dir;
                if (!_grid.IsInBounds(next) || _grid.GetTile(next) == TileType.Wall) break;
                pos = next;
            }
            return pos;
        }

        private static Direction GetDirectionToward(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;

            // Choose the axis with the greater distance
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0 ? Direction.Right : Direction.Left;
            else
                return delta.y > 0 ? Direction.Up : Direction.Down;
        }

        private static ActionType GetTurnAction(Direction current, Direction desired)
        {
            if (current == desired) return ActionType.Wait;

            var left = GridSystem.TurnLeft(current);
            var right = GridSystem.TurnRight(current);
            var around = GridSystem.TurnAround(current);

            if (left == desired) return ActionType.TurnLeft;
            if (right == desired) return ActionType.TurnRight;
            if (around == desired) return ActionType.TurnAround;

            return ActionType.TurnLeft; // fallback
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static Direction VectorToDirection(Vector2Int v)
        {
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                return v.x > 0 ? Direction.Right : Direction.Left;
            else
                return v.y > 0 ? Direction.Up : Direction.Down;
        }

        private void ApplyAction(SimState state, ActionType action)
        {
            switch (action)
            {
                case ActionType.Move:
                    var next = state.Position + GridSystem.DirectionToVector(state.Facing);
                    if (_grid.IsInBounds(next) && _grid.IsWalkable(next))
                        state.Position = next;
                    break;
                case ActionType.TurnLeft:
                    state.Facing = GridSystem.TurnLeft(state.Facing);
                    break;
                case ActionType.TurnRight:
                    state.Facing = GridSystem.TurnRight(state.Facing);
                    break;
                case ActionType.TurnAround:
                    state.Facing = GridSystem.TurnAround(state.Facing);
                    break;
                case ActionType.Shoot:
                    state.CooldownRemaining = _me.Config.cooldown;
                    break;
                case ActionType.Special:
                    state.SpecialUsed = true;
                    state.CooldownRemaining = _me.Config.cooldown;
                    break;
            }

            if (state.CooldownRemaining > 0 && action != ActionType.Shoot && action != ActionType.Special)
                state.CooldownRemaining--;
        }

        private static SimState CloneState(HeroState state)
        {
            return new SimState
            {
                Position = state.Position,
                Facing = state.Facing,
                CooldownRemaining = state.CooldownRemaining,
                SpecialUsed = state.SpecialUsedThisRound
            };
        }

        private class SimState
        {
            public Vector2Int Position;
            public Direction Facing;
            public int CooldownRemaining;
            public bool SpecialUsed;
        }

        #endregion
    }
}
