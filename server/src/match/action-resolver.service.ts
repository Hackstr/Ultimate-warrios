/**
 * TypeScript port of C# ActionResolver.
 * DETERMINISM RULE: Given identical inputs, must produce identical StepResult[] as C#.
 *
 * Resolution order per step:
 *   Phase 1 — Movement (turns, move targets, collision, pickups)
 *   Phase 2 — Combat (ray cast, hit detection)
 *   Phase 3 — Damage (mutual cancel, armor, elimination)
 *   Cooldown tick + cloak tick
 */

import { ActionType, Direction, PickupType } from '../shared/models/enums';
import {
  Vec2,
  vec2,
  vec2Eq,
  HeroConfig,
  HeroState,
  MapConfig,
  StepResult,
  createHeroState,
  createEmptyStepResult,
  heroEffectiveSpeed,
  heroEffectiveRange,
  resetHeroForNewRound,
} from '../shared/models/game-types';
import { GridSystem } from './grid-system';

export class ActionResolverService {
  private readonly _grid: GridSystem;
  private readonly _p1: HeroState;
  private readonly _p2: HeroState;

  constructor(
    map: MapConfig,
    p1Config: HeroConfig,
    p2Config: HeroConfig,
    p1Spawn: Vec2,
    p1Facing: Direction,
    p2Spawn: Vec2,
    p2Facing: Direction,
  ) {
    this._grid = new GridSystem(map);
    this._p1 = createHeroState(p1Config, 1, p1Spawn, p1Facing);
    this._p2 = createHeroState(p2Config, 2, p2Spawn, p2Facing);
  }

  // ── Public ──

  /**
   * Resolves a full round. Iterates through each step until max steps or elimination.
   */
  resolveRound(p1Actions: ActionType[], p2Actions: ActionType[]): StepResult[] {
    const maxSteps = Math.max(p1Actions.length, p2Actions.length);
    const results: StepResult[] = [];

    for (let i = 0; i < maxSteps; i++) {
      const p1Act = i < p1Actions.length ? p1Actions[i] : ActionType.Wait;
      const p2Act = i < p2Actions.length ? p2Actions[i] : ActionType.Wait;

      const step = this._resolveStep(i, p1Act, p2Act);
      results.push(step);

      if (!this._p1.isAlive || !this._p2.isAlive) break;
    }

    return results;
  }

  /** Call between rounds to reset cooldowns, bonuses, etc. */
  resetForNewRound(): void {
    resetHeroForNewRound(this._p1);
    resetHeroForNewRound(this._p2);
  }

  /** Returns current hero states for reconnection snapshots. */
  getHeroStates(): { p1: HeroState; p2: HeroState } {
    return { p1: this._p1, p2: this._p2 };
  }

  // ── Step Resolution ──

  private _resolveStep(stepIndex: number, p1Action: ActionType, p2Action: ActionType): StepResult {
    const result = createEmptyStepResult(stepIndex);
    result.p1Action = p1Action;
    result.p2Action = p2Action;
    result.p1StartPos = { ...this._p1.position };
    result.p1StartFacing = this._p1.facing;
    result.p2StartPos = { ...this._p2.position };
    result.p2StartFacing = this._p2.facing;

    this._resolveMovement(p1Action, p2Action, result);
    this._resolveCombat(p1Action, p2Action, result);
    this._resolveDamage(result);
    this._updateCooldowns(result);
    this._updateCloaking();

    result.p1EndPos = { ...this._p1.position };
    result.p1EndFacing = this._p1.facing;
    result.p2EndPos = { ...this._p2.position };
    result.p2EndFacing = this._p2.facing;

    return result;
  }

  // ── Phase 1: Movement ──

  private _resolveMovement(p1Act: ActionType, p2Act: ActionType, result: StepResult): void {
    let p1Target: Vec2 = { ...this._p1.position };
    let p2Target: Vec2 = { ...this._p2.position };

    ActionResolverService._applyTurn(this._p1, p1Act);
    ActionResolverService._applyTurn(this._p2, p2Act);

    if (p1Act === ActionType.Move) {
      p1Target = this._grid.getMoveTarget(
        this._p1.position,
        this._p1.facing,
        heroEffectiveSpeed(this._p1),
      );
    }

    if (p2Act === ActionType.Move) {
      p2Target = this._grid.getMoveTarget(
        this._p2.position,
        this._p2.facing,
        heroEffectiveSpeed(this._p2),
      );
    }

    // Collision: both moving to the same tile — both stay put
    if (
      vec2Eq(p1Target, p2Target) &&
      !vec2Eq(p1Target, this._p1.position) &&
      !vec2Eq(p2Target, this._p2.position)
    ) {
      p1Target = { ...this._p1.position };
      p2Target = { ...this._p2.position };
    }

    // Swap collision: P1→P2 pos AND P2→P1 pos
    if (vec2Eq(p1Target, this._p2.position) && vec2Eq(p2Target, this._p1.position)) {
      p1Target = { ...this._p1.position };
      p2Target = { ...this._p2.position };
    }

    // Moving into opponent's stationary position — stop before them
    if (p1Act === ActionType.Move && p2Act !== ActionType.Move && vec2Eq(p1Target, this._p2.position)) {
      p1Target = this._stopBeforeTarget(
        this._p1.position,
        this._p1.facing,
        heroEffectiveSpeed(this._p1),
        this._p2.position,
      );
    }

    if (p2Act === ActionType.Move && p1Act !== ActionType.Move && vec2Eq(p2Target, this._p1.position)) {
      p2Target = this._stopBeforeTarget(
        this._p2.position,
        this._p2.facing,
        heroEffectiveSpeed(this._p2),
        this._p1.position,
      );
    }

    this._p1.position = p1Target;
    this._p2.position = p2Target;

    this._checkPickup(this._p1, result, true);
    this._checkPickup(this._p2, result, false);
  }

  private static _applyTurn(hero: HeroState, action: ActionType): void {
    switch (action) {
      case ActionType.TurnLeft:
        hero.facing = GridSystem.turnLeft(hero.facing);
        break;
      case ActionType.TurnRight:
        hero.facing = GridSystem.turnRight(hero.facing);
        break;
      case ActionType.TurnAround:
        hero.facing = GridSystem.turnAround(hero.facing);
        break;
    }
  }

  /**
   * When moving with speed > 1 toward an occupied tile,
   * stop at the last empty tile before the blocker.
   */
  private _stopBeforeTarget(from: Vec2, dir: Direction, speed: number, blockerPos: Vec2): Vec2 {
    const step = GridSystem.directionToVector(dir);
    let cx = from.x;
    let cy = from.y;

    for (let i = 0; i < speed; i++) {
      const nx = cx + step.x;
      const ny = cy + step.y;
      const next = vec2(nx, ny);

      if (vec2Eq(next, blockerPos) || !this._grid.isWalkable(next)) break;

      cx = nx;
      cy = ny;
    }

    return vec2(cx, cy);
  }

  private _checkPickup(hero: HeroState, result: StepResult, isPlayer1: boolean): void {
    if (!this._grid.hasPickup(hero.position)) return;

    const pickup = this._grid.getPickup(hero.position);
    if (pickup === null) return;

    if (isPlayer1) {
      result.p1PickedUp = pickup;
    } else {
      result.p2PickedUp = pickup;
    }

    ActionResolverService._applyPickup(hero, pickup);
    this._grid.removePickup(hero.position);
  }

  private static _applyPickup(hero: HeroState, type: PickupType): void {
    switch (type) {
      case PickupType.ArmorShard:
        hero.hasArmor = true;
        break;
      case PickupType.IntelOrb:
        hero.hasIntel = true;
        break;
      case PickupType.SpeedBoost:
        hero.bonusSpeed = 1;
        break;
      case PickupType.RangeBoost:
        hero.bonusRange = 2;
        break;
    }
  }

  // ── Phase 2: Combat ──

  private _resolveCombat(p1Act: ActionType, p2Act: ActionType, result: StepResult): void {
    const p1Shoots = p1Act === ActionType.Shoot && this._p1.cooldownRemaining <= 0;
    const p2Shoots = p2Act === ActionType.Shoot && this._p2.cooldownRemaining <= 0;

    result.p1Fired = p1Shoots;
    result.p2Fired = p2Shoots;

    if (p1Shoots) {
      const rayTiles = this._grid.castRay(
        this._p1.position,
        this._p1.facing,
        heroEffectiveRange(this._p1),
      );
      result.p1Hit = ActionResolverService._tilesContain(rayTiles, this._p2.position);
    }

    if (p2Shoots) {
      const rayTiles = this._grid.castRay(
        this._p2.position,
        this._p2.facing,
        heroEffectiveRange(this._p2),
      );
      result.p2Hit = ActionResolverService._tilesContain(rayTiles, this._p1.position);
    }
  }

  /** Manual iteration, no allocation — matches C# behavior. */
  private static _tilesContain(tiles: Vec2[], target: Vec2): boolean {
    for (let i = 0; i < tiles.length; i++) {
      if (vec2Eq(tiles[i], target)) return true;
    }
    return false;
  }

  // ── Phase 3: Damage ──

  private _resolveDamage(result: StepResult): void {
    // Shield action: grant temporary armor for this step
    if (result.p1Action === ActionType.Shield && !this._p1.hasArmor) {
      this._p1.hasArmor = true;
      result.p1Shielded = true;
    }
    if (result.p2Action === ActionType.Shield && !this._p2.hasArmor) {
      this._p2.hasArmor = true;
      result.p2Shielded = true;
    }

    // Mutual cancel: both shots hit — both nullified
    if (result.p1Hit && result.p2Hit) {
      result.mutualCancel = true;
      result.p1Hit = false;
      result.p2Hit = false;
      // Remove shield-granted armor if no hit absorbed it
      if (result.p1Shielded) this._p1.hasArmor = false;
      if (result.p2Shielded) this._p2.hasArmor = false;
      return;
    }

    if (result.p1Hit) {
      ActionResolverService._applyDamage(this._p2, result, false);
    }

    if (result.p2Hit) {
      ActionResolverService._applyDamage(this._p1, result, true);
    }

    // Remove shield-granted armor after damage resolution (temporary, one-step only)
    if (result.p1Shielded && !result.p1ArmorBroken)
      this._p1.hasArmor = false;
    if (result.p2Shielded && !result.p2ArmorBroken)
      this._p2.hasArmor = false;
  }

  private static _applyDamage(target: HeroState, result: StepResult, isTarget1: boolean): void {
    if (target.hasArmor) {
      target.hasArmor = false;
      if (isTarget1) result.p1ArmorBroken = true;
      else result.p2ArmorBroken = true;
    } else {
      target.isAlive = false;
      if (isTarget1) result.p1Eliminated = true;
      else result.p2Eliminated = true;
    }
  }

  // ── Cooldown & State Updates ──

  private _updateCooldowns(result: StepResult): void {
    if (result.p1Fired) {
      this._p1.cooldownRemaining = this._p1.config.cooldown;
    }
    if (result.p2Fired) {
      this._p2.cooldownRemaining = this._p2.config.cooldown;
    }

    this._p1.cooldownRemaining = Math.max(0, this._p1.cooldownRemaining - 1);
    this._p2.cooldownRemaining = Math.max(0, this._p2.cooldownRemaining - 1);
  }

  private _updateCloaking(): void {
    ActionResolverService._tickCloak(this._p1);
    ActionResolverService._tickCloak(this._p2);
  }

  private static _tickCloak(hero: HeroState): void {
    if (!hero.isCloaked) return;
    hero.cloakStepsRemaining--;
    if (hero.cloakStepsRemaining <= 0) {
      hero.isCloaked = false;
      hero.cloakStepsRemaining = 0;
    }
  }
}
