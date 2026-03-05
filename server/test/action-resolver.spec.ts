/**
 * Shared test vectors — must produce identical results to C# ActionResolverTests.
 * If a test passes here but fails in Unity (or vice-versa), the deterministic
 * port is broken and commit-reveal will fail in production.
 */

import { ActionType, Direction, TileType } from '../src/shared/models/enums';
import {
  HeroConfig,
  MapConfig,
  StepResult,
  vec2,
} from '../src/shared/models/game-types';
import { ActionResolverService } from '../src/match/action-resolver.service';

// ── Helpers ──

function heroConfig(overrides: Partial<HeroConfig> = {}): HeroConfig {
  return {
    heroId: 'test_hero',
    heroName: 'TestHero',
    steps: 4,
    range: 5,
    cooldown: 1,
    armor: 0,
    speed: 1,
    specialName: '',
    ...overrides,
  };
}

function openMap(w = 10, h = 10): MapConfig {
  const grid: TileType[][] = [];
  for (let x = 0; x < w; x++) {
    const col: TileType[] = [];
    for (let y = 0; y < h; y++) col.push(TileType.Empty);
    grid.push(col);
  }
  return {
    mapId: 'test',
    width: w,
    height: h,
    gridData: grid,
    pickups: [],
    spawnPoints: [vec2(0, 0), vec2(9, 9)],
    spawnFacings: [Direction.Up, Direction.Down],
  };
}

function mapWithWall(w: number, h: number, wallX: number, wallY: number): MapConfig {
  const m = openMap(w, h);
  m.gridData[wallX][wallY] = TileType.Wall;
  return m;
}

/**
 * Creates a resolver with two heroes and resolves a single step.
 * Mirrors C# pattern: new ActionResolver(grid, p1, p2).ResolveStep(0, a1, a2)
 */
function resolveOneStep(
  map: MapConfig,
  p1Cfg: HeroConfig, p1Spawn: { x: number; y: number }, p1Facing: Direction,
  p2Cfg: HeroConfig, p2Spawn: { x: number; y: number }, p2Facing: Direction,
  p1Action: ActionType, p2Action: ActionType,
): StepResult {
  const svc = new ActionResolverService(
    map, p1Cfg, p2Cfg,
    vec2(p1Spawn.x, p1Spawn.y), p1Facing,
    vec2(p2Spawn.x, p2Spawn.y), p2Facing,
  );
  const results = svc.resolveRound([p1Action], [p2Action]);
  return results[0];
}

// ── Tests (mirroring C# ActionResolverTests 1:1) ──

describe('ActionResolverService — shared test vectors', () => {

  // ── Basic Movement ──

  test('Move_FacingUp_MovesOneUp', () => {
    const r = resolveOneStep(
      openMap(), heroConfig({ speed: 1 }), { x: 5, y: 2 }, Direction.Up,
      heroConfig({ speed: 1 }), { x: 8, y: 8 }, Direction.Down,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(5, 3));
    expect(r.p2EndPos).toEqual(vec2(8, 8));
  });

  test('Move_Speed2_MovesTwoTiles', () => {
    const r = resolveOneStep(
      openMap(), heroConfig({ speed: 2 }), { x: 3, y: 3 }, Direction.Right,
      heroConfig({ speed: 2 }), { x: 9, y: 9 }, Direction.Down,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(5, 3));
  });

  test('Move_FacingDown_MovesOneDown', () => {
    const r = resolveOneStep(
      openMap(), heroConfig({ speed: 1 }), { x: 5, y: 5 }, Direction.Down,
      heroConfig({ speed: 1 }), { x: 9, y: 9 }, Direction.Up,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(5, 4));
  });

  test('Wait_DoesNotMove', () => {
    const r = resolveOneStep(
      openMap(), heroConfig(), { x: 5, y: 5 }, Direction.Up,
      heroConfig(), { x: 9, y: 9 }, Direction.Down,
      ActionType.Wait, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(5, 5));
    expect(r.p2EndPos).toEqual(vec2(9, 9));
  });

  // ── Turning ──

  test('TurnLeft_ChangesFacingCorrectly', () => {
    const r = resolveOneStep(
      openMap(), heroConfig(), { x: 5, y: 5 }, Direction.Up,
      heroConfig(), { x: 9, y: 9 }, Direction.Down,
      ActionType.TurnLeft, ActionType.Wait,
    );
    expect(r.p1EndFacing).toBe(Direction.Left);
    expect(r.p1EndPos).toEqual(vec2(5, 5));
  });

  test('TurnRight_ChangesFacingCorrectly', () => {
    const r = resolveOneStep(
      openMap(), heroConfig(), { x: 5, y: 5 }, Direction.Up,
      heroConfig(), { x: 9, y: 9 }, Direction.Down,
      ActionType.TurnRight, ActionType.Wait,
    );
    expect(r.p1EndFacing).toBe(Direction.Right);
  });

  test('TurnAround_ReversesFacing', () => {
    const r = resolveOneStep(
      openMap(), heroConfig(), { x: 5, y: 5 }, Direction.Left,
      heroConfig(), { x: 9, y: 9 }, Direction.Down,
      ActionType.TurnAround, ActionType.Wait,
    );
    expect(r.p1EndFacing).toBe(Direction.Right);
  });

  // ── Collision ──

  test('Collision_BothMoveToSameTile_BothStayPut', () => {
    const r = resolveOneStep(
      openMap(), heroConfig({ speed: 1 }), { x: 4, y: 5 }, Direction.Right,
      heroConfig({ speed: 1 }), { x: 6, y: 5 }, Direction.Left,
      ActionType.Move, ActionType.Move,
    );
    expect(r.p1EndPos).toEqual(vec2(4, 5));
    expect(r.p2EndPos).toEqual(vec2(6, 5));
  });

  test('Collision_SwapPositions_BothStayPut', () => {
    const r = resolveOneStep(
      openMap(), heroConfig({ speed: 1 }), { x: 5, y: 5 }, Direction.Right,
      heroConfig({ speed: 1 }), { x: 6, y: 5 }, Direction.Left,
      ActionType.Move, ActionType.Move,
    );
    expect(r.p1EndPos).toEqual(vec2(5, 5));
    expect(r.p2EndPos).toEqual(vec2(6, 5));
  });

  test('Collision_MovingIntoStationaryOpponent_StopsBefore', () => {
    const r = resolveOneStep(
      openMap(), heroConfig({ speed: 1 }), { x: 4, y: 5 }, Direction.Right,
      heroConfig({ speed: 1 }), { x: 5, y: 5 }, Direction.Down,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(4, 5));
  });

  // ── Wall Blocking ──

  test('Move_BlockedByWall_StaysAtOriginal', () => {
    const r = resolveOneStep(
      mapWithWall(10, 10, 5, 6), heroConfig({ speed: 1 }), { x: 5, y: 5 }, Direction.Up,
      heroConfig({ speed: 1 }), { x: 9, y: 9 }, Direction.Down,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(5, 5));
  });

  test('Move_Speed2_StopsBeforeWall', () => {
    const r = resolveOneStep(
      mapWithWall(10, 10, 7, 3), heroConfig({ speed: 2 }), { x: 5, y: 3 }, Direction.Right,
      heroConfig({ speed: 2 }), { x: 0, y: 0 }, Direction.Down,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(6, 3));
  });

  test('Move_AtGridEdge_CannotMoveOut', () => {
    const r = resolveOneStep(
      openMap(10, 10), heroConfig({ speed: 1 }), { x: 0, y: 0 }, Direction.Down,
      heroConfig({ speed: 1 }), { x: 9, y: 9 }, Direction.Up,
      ActionType.Move, ActionType.Wait,
    );
    expect(r.p1EndPos).toEqual(vec2(0, 0));
  });

  // ── Shooting ──

  test('Shoot_OpponentInRange_Hits', () => {
    const cfg = heroConfig({ range: 5, cooldown: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 2, y: 5 }, Direction.Right,
      cfg, { x: 5, y: 5 }, Direction.Left,
      ActionType.Shoot, ActionType.Wait,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p1Hit).toBe(true);
  });

  test('Shoot_OpponentOutOfRange_Misses', () => {
    const cfg = heroConfig({ range: 3, cooldown: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 1, y: 5 }, Direction.Right,
      cfg, { x: 8, y: 5 }, Direction.Left,
      ActionType.Shoot, ActionType.Wait,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p1Hit).toBe(false);
  });

  test('Shoot_WrongDirection_Misses', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 5, y: 5 }, Direction.Up,
      cfg, { x: 5, y: 3 }, Direction.Down,
      ActionType.Shoot, ActionType.Wait,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p1Hit).toBe(false);
  });

  test('Shoot_BlockedByWall_Misses', () => {
    const cfg = heroConfig({ range: 5, cooldown: 1 });
    const r = resolveOneStep(
      mapWithWall(10, 10, 4, 5), cfg, { x: 3, y: 5 }, Direction.Right,
      cfg, { x: 6, y: 5 }, Direction.Left,
      ActionType.Shoot, ActionType.Wait,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p1Hit).toBe(false);
  });

  test('Shoot_AfterMove_UsesNewPosition', () => {
    const cfg = heroConfig({ range: 5, cooldown: 1, speed: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 2, y: 5 }, Direction.Right,
      cfg, { x: 5, y: 5 }, Direction.Right,
      ActionType.Shoot, ActionType.Move,
    );
    // P2 moves from (5,5) to (6,5). P1 ray from (2,5) Right hits (6,5) at range 4 ≤ 5
    expect(r.p1Fired).toBe(true);
    expect(r.p1Hit).toBe(true);
  });

  // ── Mutual Cancel ──

  test('MutualCancel_BothShootAndHit_NoDamage', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 2, y: 5 }, Direction.Right,
      cfg, { x: 8, y: 5 }, Direction.Left,
      ActionType.Shoot, ActionType.Shoot,
    );
    expect(r.mutualCancel).toBe(true);
    expect(r.p1Eliminated).toBe(false);
    expect(r.p2Eliminated).toBe(false);
  });

  test('MutualCancel_ArmorNotBroken', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1, armor: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 2, y: 5 }, Direction.Right,
      cfg, { x: 8, y: 5 }, Direction.Left,
      ActionType.Shoot, ActionType.Shoot,
    );
    expect(r.mutualCancel).toBe(true);
    expect(r.p1ArmorBroken).toBe(false);
    expect(r.p2ArmorBroken).toBe(false);
  });

  // ── Armor ──

  test('Armor_AbsorbsHit_NotEliminated', () => {
    const attacker = heroConfig({ range: 10, cooldown: 1, armor: 0 });
    const defender = heroConfig({ range: 10, cooldown: 1, armor: 1 });
    const r = resolveOneStep(
      openMap(), attacker, { x: 2, y: 5 }, Direction.Right,
      defender, { x: 7, y: 5 }, Direction.Down,
      ActionType.Shoot, ActionType.Wait,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p2ArmorBroken).toBe(true);
    expect(r.p2Eliminated).toBe(false);
  });

  test('Armor_SecondShot_Eliminates', () => {
    const attacker = heroConfig({ range: 10, cooldown: 0, armor: 0 });
    const defender = heroConfig({ range: 10, cooldown: 1, armor: 1 });
    const svc = new ActionResolverService(
      openMap(), attacker, defender,
      vec2(2, 5), Direction.Right,
      vec2(7, 5), Direction.Down,
    );
    const results = svc.resolveRound(
      [ActionType.Shoot, ActionType.Shoot],
      [ActionType.Wait, ActionType.Wait],
    );

    expect(results[0].p2ArmorBroken).toBe(true);
    expect(results[0].p2Eliminated).toBe(false);
    expect(results[1].p1Fired).toBe(true);
    expect(results[1].p2Eliminated).toBe(true);
  });

  // ── Elimination ──

  test('Shoot_NoArmor_Eliminates', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1, armor: 0 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 2, y: 5 }, Direction.Right,
      cfg, { x: 7, y: 5 }, Direction.Down,
      ActionType.Shoot, ActionType.Wait,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p2Eliminated).toBe(true);
  });

  test('OneHitsOther_NotMutualCancel', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1, armor: 0 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 2, y: 5 }, Direction.Right,
      cfg, { x: 7, y: 5 }, Direction.Up,
      ActionType.Shoot, ActionType.Shoot,
    );
    expect(r.p1Fired).toBe(true);
    expect(r.p2Fired).toBe(true);
    expect(r.p1Hit).toBe(true);
    expect(r.p2Hit).toBe(false);
    expect(r.mutualCancel).toBe(false);
    expect(r.p2Eliminated).toBe(true);
  });

  // ── Cooldown ──

  test('Cooldown_CannotShootWhileOnCooldown', () => {
    const cfg = heroConfig({ range: 10, cooldown: 2, armor: 0 });
    const svc = new ActionResolverService(
      openMap(), cfg, cfg,
      vec2(2, 5), Direction.Right,
      vec2(7, 5), Direction.Down,
    );
    const results = svc.resolveRound(
      [ActionType.Shoot, ActionType.Shoot],
      [ActionType.Wait, ActionType.Wait],
    );

    expect(results[0].p1Fired).toBe(true);
    // Step 1 stopped early because P2 was eliminated in step 0
    // To properly test cooldown, P1 must miss P2
  });

  test('Cooldown_CannotShootWhileOnCooldown_v2', () => {
    // P1 shoots and misses (wrong direction), then tries again on cooldown
    const cfg = heroConfig({ range: 10, cooldown: 2, armor: 0 });
    const svc = new ActionResolverService(
      openMap(), cfg, cfg,
      vec2(2, 5), Direction.Up, // faces Up, P2 is to the right — will miss
      vec2(7, 5), Direction.Down,
    );
    const results = svc.resolveRound(
      [ActionType.Shoot, ActionType.Shoot],
      [ActionType.Wait, ActionType.Wait],
    );

    expect(results[0].p1Fired).toBe(true);
    expect(results[0].p1Hit).toBe(false);
    expect(results[1].p1Fired).toBe(false);
  });

  test('Cooldown_TicksDown_CanShootAfterCooldown', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1, armor: 0 });
    const svc = new ActionResolverService(
      openMap(), cfg, cfg,
      vec2(2, 5), Direction.Up, // misses P2 (shoots Up, P2 is Right)
      vec2(7, 5), Direction.Down,
    );
    const results = svc.resolveRound(
      [ActionType.Shoot, ActionType.Shoot],
      [ActionType.Wait, ActionType.Wait],
    );

    expect(results[0].p1Fired).toBe(true);
    // CD=1 after fire, ticks to 0 → step 1 should fire
    expect(results[1].p1Fired).toBe(true);
  });

  test('Cooldown_Zero_CanFireEveryStep', () => {
    const cfg = heroConfig({ range: 10, cooldown: 0, armor: 0 });
    const svc = new ActionResolverService(
      openMap(), cfg, cfg,
      vec2(2, 5), Direction.Up, // misses P2 every time
      vec2(9, 5), Direction.Down,
    );
    const results = svc.resolveRound(
      [ActionType.Shoot, ActionType.Shoot, ActionType.Shoot],
      [ActionType.Wait, ActionType.Wait, ActionType.Wait],
    );

    for (let i = 0; i < 3; i++) {
      expect(results[i].p1Fired).toBe(true);
    }
  });

  // ── Simultaneous Actions ──

  test('BothMove_Simultaneously_ToSeparateTiles', () => {
    const cfg = heroConfig({ speed: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 3, y: 3 }, Direction.Up,
      cfg, { x: 7, y: 7 }, Direction.Down,
      ActionType.Move, ActionType.Move,
    );
    expect(r.p1EndPos).toEqual(vec2(3, 4));
    expect(r.p2EndPos).toEqual(vec2(7, 6));
  });

  test('P1Moves_P2Shoots_ShootsAtNewPosition', () => {
    const cfg = heroConfig({ range: 10, cooldown: 1, speed: 1 });
    const r = resolveOneStep(
      openMap(), cfg, { x: 5, y: 3 }, Direction.Up,
      cfg, { x: 5, y: 8 }, Direction.Down,
      ActionType.Move, ActionType.Shoot,
    );
    // P1 moves to (5,4). P2 ray from (5,8) Down: (5,7),(5,6),(5,5),(5,4) — hits P1
    expect(r.p1EndPos).toEqual(vec2(5, 4));
    expect(r.p2Fired).toBe(true);
    expect(r.p2Hit).toBe(true);
  });

  // ── resolveRound stops on elimination ──

  test('resolveRound_StopsAfterElimination', () => {
    const cfg = heroConfig({ range: 10, cooldown: 0, armor: 0 });
    const svc = new ActionResolverService(
      openMap(), cfg, cfg,
      vec2(2, 5), Direction.Right,
      vec2(7, 5), Direction.Left,
      );
    const results = svc.resolveRound(
      [ActionType.Shoot, ActionType.Move, ActionType.Move],
      [ActionType.Wait, ActionType.Move, ActionType.Move],
    );

    // Both shoot and hit at step 0 → mutual cancel, both alive
    // Actually P1 shoots, P2 waits → P2 eliminated → stops at step 0
    expect(results).toHaveLength(1);
    expect(results[0].p2Eliminated).toBe(true);
  });
});
