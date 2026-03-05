/**
 * Shared test vectors — must produce identical results to C# GridSystemTests.
 * Covers: IsWalkable, IsInBounds, CastRay, GetMoveTarget, direction helpers,
 * DangerZone, and grid modifications.
 */

import { Direction, TileType } from '../src/shared/models/enums';
import { vec2, MapConfig } from '../src/shared/models/game-types';
import { GridSystem } from '../src/match/grid-system';

// ── Helpers ──

function openMap(w = 10, h = 10): MapConfig {
  const grid: TileType[][] = [];
  for (let x = 0; x < w; x++) {
    const col: TileType[] = [];
    for (let y = 0; y < h; y++) col.push(TileType.Empty);
    grid.push(col);
  }
  return {
    mapId: 'test', width: w, height: h,
    gridData: grid, pickups: [],
    spawnPoints: [vec2(0, 0)], spawnFacings: [Direction.Up],
  };
}

function mapWithWalls(w: number, h: number, ...walls: { x: number; y: number }[]): MapConfig {
  const m = openMap(w, h);
  for (const wall of walls) m.gridData[wall.x][wall.y] = TileType.Wall;
  return m;
}

function gs(map: MapConfig): GridSystem { return new GridSystem(map); }

// ── Tests ──

describe('GridSystem — shared test vectors', () => {

  // ── IsWalkable ──

  test('IsWalkable_EmptyTile_True', () => {
    expect(gs(openMap()).isWalkable(vec2(5, 5))).toBe(true);
  });

  test('IsWalkable_WallTile_False', () => {
    expect(gs(mapWithWalls(10, 10, { x: 3, y: 3 })).isWalkable(vec2(3, 3))).toBe(false);
  });

  test('IsWalkable_OutOfBounds_False', () => {
    const g = gs(openMap(10, 10));
    expect(g.isWalkable(vec2(-1, 5))).toBe(false);
    expect(g.isWalkable(vec2(5, -1))).toBe(false);
    expect(g.isWalkable(vec2(10, 5))).toBe(false);
    expect(g.isWalkable(vec2(5, 10))).toBe(false);
  });

  test('IsWalkable_Corners_True', () => {
    const g = gs(openMap(10, 10));
    expect(g.isWalkable(vec2(0, 0))).toBe(true);
    expect(g.isWalkable(vec2(9, 0))).toBe(true);
    expect(g.isWalkable(vec2(0, 9))).toBe(true);
    expect(g.isWalkable(vec2(9, 9))).toBe(true);
  });

  // ── IsInBounds ──

  test('IsInBounds_ValidPositions_True', () => {
    const g = gs(openMap(10, 10));
    expect(g.isInBounds(vec2(0, 0))).toBe(true);
    expect(g.isInBounds(vec2(9, 9))).toBe(true);
    expect(g.isInBounds(vec2(5, 5))).toBe(true);
  });

  test('IsInBounds_NegativeCoords_False', () => {
    const g = gs(openMap(10, 10));
    expect(g.isInBounds(vec2(-1, 0))).toBe(false);
    expect(g.isInBounds(vec2(0, -1))).toBe(false);
    expect(g.isInBounds(vec2(-5, -5))).toBe(false);
  });

  test('IsInBounds_ExactBoundary_False', () => {
    const g = gs(openMap(10, 10));
    expect(g.isInBounds(vec2(10, 0))).toBe(false);
    expect(g.isInBounds(vec2(0, 10))).toBe(false);
    expect(g.isInBounds(vec2(10, 10))).toBe(false);
  });

  // ── CastRay — Open Grid ──

  test('CastRay_Open_RightRange5_Returns5Tiles', () => {
    const tiles = gs(openMap(10, 10)).castRay(vec2(2, 5), Direction.Right, 5);
    expect(tiles).toHaveLength(5);
    expect(tiles[0]).toEqual(vec2(3, 5));
    expect(tiles[1]).toEqual(vec2(4, 5));
    expect(tiles[2]).toEqual(vec2(5, 5));
    expect(tiles[3]).toEqual(vec2(6, 5));
    expect(tiles[4]).toEqual(vec2(7, 5));
  });

  test('CastRay_Open_UpRange3_Returns3Tiles', () => {
    const tiles = gs(openMap(10, 10)).castRay(vec2(5, 3), Direction.Up, 3);
    expect(tiles).toHaveLength(3);
    expect(tiles[0]).toEqual(vec2(5, 4));
    expect(tiles[1]).toEqual(vec2(5, 5));
    expect(tiles[2]).toEqual(vec2(5, 6));
  });

  test('CastRay_DoesNotIncludeStartTile', () => {
    const tiles = gs(openMap()).castRay(vec2(5, 5), Direction.Up, 2);
    for (const t of tiles) {
      expect(t).not.toEqual(vec2(5, 5));
    }
  });

  test('CastRay_StopsAtGridEdge', () => {
    const tiles = gs(openMap(10, 10)).castRay(vec2(8, 5), Direction.Right, 10);
    expect(tiles).toHaveLength(1);
    expect(tiles[0]).toEqual(vec2(9, 5));
  });

  // ── CastRay — Wall Blocked ──

  test('CastRay_WallBlocked_StopsBeforeWall', () => {
    const tiles = gs(mapWithWalls(10, 10, { x: 5, y: 5 })).castRay(vec2(2, 5), Direction.Right, 8);
    expect(tiles).toHaveLength(2);
    expect(tiles[0]).toEqual(vec2(3, 5));
    expect(tiles[1]).toEqual(vec2(4, 5));
  });

  test('CastRay_WallImmediatelyAdjacent_ReturnsEmpty', () => {
    const tiles = gs(mapWithWalls(10, 10, { x: 3, y: 5 })).castRay(vec2(2, 5), Direction.Right, 5);
    expect(tiles).toHaveLength(0);
  });

  test('CastRay_DoesNotIncludeWallTile', () => {
    const tiles = gs(mapWithWalls(10, 10, { x: 6, y: 5 })).castRay(vec2(3, 5), Direction.Right, 10);
    for (const t of tiles) {
      expect(t).not.toEqual(vec2(6, 5));
    }
  });

  // ── GetMoveTarget ──

  test('GetMoveTarget_OpenGrid_Speed1_MovesOneTile', () => {
    expect(gs(openMap()).getMoveTarget(vec2(5, 5), Direction.Up, 1)).toEqual(vec2(5, 6));
  });

  test('GetMoveTarget_OpenGrid_Speed2_MovesTwoTiles', () => {
    expect(gs(openMap()).getMoveTarget(vec2(5, 5), Direction.Right, 2)).toEqual(vec2(7, 5));
  });

  test('GetMoveTarget_WallBlocked_StopsBeforeWall', () => {
    const target = gs(mapWithWalls(10, 10, { x: 5, y: 7 })).getMoveTarget(vec2(5, 5), Direction.Up, 2);
    expect(target).toEqual(vec2(5, 6));
  });

  test('GetMoveTarget_WallDirectlyAhead_StaysInPlace', () => {
    const target = gs(mapWithWalls(10, 10, { x: 5, y: 6 })).getMoveTarget(vec2(5, 5), Direction.Up, 1);
    expect(target).toEqual(vec2(5, 5));
  });

  test('GetMoveTarget_AtGridEdge_StaysInPlace', () => {
    const target = gs(openMap(10, 10)).getMoveTarget(vec2(5, 9), Direction.Up, 1);
    expect(target).toEqual(vec2(5, 9));
  });

  test('GetMoveTarget_Speed2_PartiallyBlocked', () => {
    const target = gs(mapWithWalls(10, 10, { x: 5, y: 8 })).getMoveTarget(vec2(5, 5), Direction.Up, 3);
    expect(target).toEqual(vec2(5, 7));
  });

  test('GetMoveTarget_AllDirections', () => {
    const g = gs(openMap());
    const c = vec2(5, 5);
    expect(g.getMoveTarget(c, Direction.Up, 1)).toEqual(vec2(5, 6));
    expect(g.getMoveTarget(c, Direction.Down, 1)).toEqual(vec2(5, 4));
    expect(g.getMoveTarget(c, Direction.Left, 1)).toEqual(vec2(4, 5));
    expect(g.getMoveTarget(c, Direction.Right, 1)).toEqual(vec2(6, 5));
  });

  // ── Direction Helpers ──

  test('directionToVector_AllDirections', () => {
    expect(GridSystem.directionToVector(Direction.Up)).toEqual(vec2(0, 1));
    expect(GridSystem.directionToVector(Direction.Down)).toEqual(vec2(0, -1));
    expect(GridSystem.directionToVector(Direction.Left)).toEqual(vec2(-1, 0));
    expect(GridSystem.directionToVector(Direction.Right)).toEqual(vec2(1, 0));
  });

  test('TurnLeft_CyclesCorrectly', () => {
    expect(GridSystem.turnLeft(Direction.Up)).toBe(Direction.Left);
    expect(GridSystem.turnLeft(Direction.Left)).toBe(Direction.Down);
    expect(GridSystem.turnLeft(Direction.Down)).toBe(Direction.Right);
    expect(GridSystem.turnLeft(Direction.Right)).toBe(Direction.Up);
  });

  test('TurnRight_CyclesCorrectly', () => {
    expect(GridSystem.turnRight(Direction.Up)).toBe(Direction.Right);
    expect(GridSystem.turnRight(Direction.Right)).toBe(Direction.Down);
    expect(GridSystem.turnRight(Direction.Down)).toBe(Direction.Left);
    expect(GridSystem.turnRight(Direction.Left)).toBe(Direction.Up);
  });

  test('TurnAround_ReversesAllDirections', () => {
    expect(GridSystem.turnAround(Direction.Up)).toBe(Direction.Down);
    expect(GridSystem.turnAround(Direction.Down)).toBe(Direction.Up);
    expect(GridSystem.turnAround(Direction.Left)).toBe(Direction.Right);
    expect(GridSystem.turnAround(Direction.Right)).toBe(Direction.Left);
  });

  test('VectorToDirection_AllVectors', () => {
    expect(GridSystem.vectorToDirection(vec2(0, 1))).toBe(Direction.Up);
    expect(GridSystem.vectorToDirection(vec2(0, -1))).toBe(Direction.Down);
    expect(GridSystem.vectorToDirection(vec2(-1, 0))).toBe(Direction.Left);
    expect(GridSystem.vectorToDirection(vec2(1, 0))).toBe(Direction.Right);
  });

  // ── DangerZone ──

  test('DangerZone_IsWalkable_ButMarkedDanger', () => {
    const g = gs(openMap(10, 10));
    g.expandDangerZone([vec2(3, 3)]);

    expect(g.isWalkable(vec2(3, 3))).toBe(true);
    expect(g.isInDangerZone(vec2(3, 3))).toBe(true);
    expect(g.getTile(vec2(3, 3))).toBe(TileType.DangerZone);
  });

  test('DangerZone_DestroysDestructibleWalls_RayPassesThrough', () => {
    const m = openMap(10, 10);
    m.gridData[4][5] = TileType.DestructibleWall;
    const g = gs(m);

    // Before: ray blocked by destructible wall at (4,5)
    const rayBefore = g.castRay(vec2(2, 5), Direction.Right, 5);
    expect(rayBefore).toHaveLength(1);

    g.expandDangerZone([vec2(4, 5)]);

    // After: wall destroyed, ray passes through
    const rayAfter = g.castRay(vec2(2, 5), Direction.Right, 5);
    expect(rayAfter).toHaveLength(5);
  });

  // ── Grid Modifications ──

  test('PlaceBarrier_MakesTileUnwalkable', () => {
    const g = gs(openMap());
    expect(g.isWalkable(vec2(5, 5))).toBe(true);
    g.placeBarrier(vec2(5, 5));
    expect(g.isWalkable(vec2(5, 5))).toBe(false);
  });

  test('RemoveBarrier_MakesTileWalkable', () => {
    const g = gs(mapWithWalls(10, 10, { x: 5, y: 5 }));
    expect(g.isWalkable(vec2(5, 5))).toBe(false);
    g.removeBarrier(vec2(5, 5));
    expect(g.isWalkable(vec2(5, 5))).toBe(true);
  });
});
