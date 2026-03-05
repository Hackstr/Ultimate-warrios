/**
 * TypeScript port of C# GridSystem.
 * Coordinate convention: (col, row) = (x, y), origin at bottom-left.
 * Grid storage: _grid[x][y] — column-major, identical to C# _grid[col, row].
 *
 * DETERMINISM RULE: This must produce identical results to the C# version.
 */

import { Direction, TileType, PickupType } from '../shared/models/enums';
import {
  Vec2,
  vec2,
  vec2Eq,
  vec2Add,
  MapConfig,
} from '../shared/models/game-types';

export class GridSystem {
  private readonly _grid: TileType[][];
  private readonly _pickups: Map<string, PickupType>;
  private readonly _dangerZone: Set<string>;
  private readonly _rayBuffer: Vec2[] = [];

  public readonly width: number;
  public readonly height: number;

  constructor(config: MapConfig) {
    this.width = config.width;
    this.height = config.height;

    this._grid = [];
    for (let x = 0; x < this.width; x++) {
      this._grid[x] = [];
      for (let y = 0; y < this.height; y++) {
        this._grid[x][y] = config.gridData[x][y];
      }
    }

    this._pickups = new Map();
    this._dangerZone = new Set();

    if (config.pickups) {
      for (const p of config.pickups) {
        this._pickups.set(this._key(p.position), p.type);
      }
    }
  }

  // ── Tile Queries ──

  getTile(pos: Vec2): TileType {
    if (!this.isInBounds(pos)) return TileType.OutOfBounds;
    if (this._dangerZone.has(this._key(pos))) return TileType.DangerZone;
    return this._grid[pos.x][pos.y];
  }

  isInBounds(pos: Vec2): boolean {
    return pos.x >= 0 && pos.x < this.width && pos.y >= 0 && pos.y < this.height;
  }

  /**
   * Walkable = in bounds AND not Wall AND not OutOfBounds.
   * DestructibleWall IS walkable (matches C# behavior).
   * DangerZone IS walkable (heroes walk through but die at round end).
   */
  isWalkable(pos: Vec2): boolean {
    if (!this.isInBounds(pos)) return false;
    const tile = this._grid[pos.x][pos.y];
    return tile !== TileType.Wall && tile !== TileType.OutOfBounds;
  }

  isInDangerZone(pos: Vec2): boolean {
    return this._dangerZone.has(this._key(pos));
  }

  // ── Pickup Queries ──

  hasPickup(pos: Vec2): boolean {
    return this._pickups.has(this._key(pos));
  }

  getPickup(pos: Vec2): PickupType | null {
    return this._pickups.get(this._key(pos)) ?? null;
  }

  removePickup(pos: Vec2): void {
    this._pickups.delete(this._key(pos));
  }

  // ── Ray Casting ──

  /**
   * Casts a ray from position in direction for up to range tiles.
   * Returns tiles the ray passes through (NOT including start pos).
   * Stops at first Wall or DestructibleWall — does NOT include it.
   * WARNING: returns internal buffer — copy if you need to keep it.
   */
  castRay(from: Vec2, dir: Direction, range: number): Vec2[] {
    this._rayBuffer.length = 0;
    const step = GridSystem.directionToVector(dir);
    let cx = from.x;
    let cy = from.y;

    for (let i = 0; i < range; i++) {
      cx += step.x;
      cy += step.y;

      if (cx < 0 || cx >= this.width || cy < 0 || cy >= this.height) break;

      const tile = this._grid[cx][cy];
      if (tile === TileType.Wall || tile === TileType.DestructibleWall) break;

      this._rayBuffer.push(vec2(cx, cy));
    }
    return this._rayBuffer;
  }

  /** Mage PhaseShot — passes through exactly 1 wall. */
  castRayPhase(from: Vec2, dir: Direction, range: number): Vec2[] {
    this._rayBuffer.length = 0;
    const step = GridSystem.directionToVector(dir);
    let cx = from.x;
    let cy = from.y;
    let wallsPassed = 0;

    for (let i = 0; i < range; i++) {
      cx += step.x;
      cy += step.y;

      if (cx < 0 || cx >= this.width || cy < 0 || cy >= this.height) break;

      const tile = this._grid[cx][cy];
      if (tile === TileType.Wall || tile === TileType.DestructibleWall) {
        wallsPassed++;
        if (wallsPassed > 1) break;
        continue;
      }

      this._rayBuffer.push(vec2(cx, cy));
    }
    return this._rayBuffer;
  }

  /** Hawk Pierce — passes through ALL walls. */
  castRayPierce(from: Vec2, dir: Direction, range: number): Vec2[] {
    this._rayBuffer.length = 0;
    const step = GridSystem.directionToVector(dir);
    let cx = from.x;
    let cy = from.y;

    for (let i = 0; i < range; i++) {
      cx += step.x;
      cy += step.y;

      if (cx < 0 || cx >= this.width || cy < 0 || cy >= this.height) break;

      this._rayBuffer.push(vec2(cx, cy));
    }
    return this._rayBuffer;
  }

  /** Archer Ricochet — bounces off 1 wall, clockwise preference. */
  castRayRicochet(from: Vec2, dir: Direction, range: number): Vec2[] {
    this._rayBuffer.length = 0;
    let step = GridSystem.directionToVector(dir);
    let cx = from.x;
    let cy = from.y;
    let hasBounced = false;
    let currentDir = dir;

    for (let i = 0; i < range; i++) {
      const nx = cx + step.x;
      const ny = cy + step.y;

      if (nx < 0 || nx >= this.width || ny < 0 || ny >= this.height) break;

      const tile = this._grid[nx][ny];
      if (tile === TileType.Wall || tile === TileType.DestructibleWall) {
        if (hasBounced) break;

        hasBounced = true;
        const cwDir = GridSystem.turnRight(currentDir);
        const cwStep = GridSystem.directionToVector(cwDir);
        const cwNext = vec2(cx + cwStep.x, cy + cwStep.y);

        if (this.isInBounds(cwNext) && this.isWalkable(cwNext)) {
          currentDir = cwDir;
          step = cwStep;
        } else {
          const ccwDir = GridSystem.turnLeft(currentDir);
          const ccwStep = GridSystem.directionToVector(ccwDir);
          const ccwNext = vec2(cx + ccwStep.x, cy + ccwStep.y);

          if (this.isInBounds(ccwNext) && this.isWalkable(ccwNext)) {
            currentDir = ccwDir;
            step = ccwStep;
          } else {
            currentDir = GridSystem.turnAround(currentDir);
            step = GridSystem.directionToVector(currentDir);
          }
        }

        i--;
        continue;
      }

      cx = nx;
      cy = ny;
      this._rayBuffer.push(vec2(cx, cy));
    }
    return this._rayBuffer;
  }

  // ── Movement ──

  /**
   * Calculates move target. Stops at last walkable tile before wall.
   * Does NOT check for other heroes — collision is ActionResolver's job.
   */
  getMoveTarget(from: Vec2, dir: Direction, speed: number): Vec2 {
    const step = GridSystem.directionToVector(dir);
    let cx = from.x;
    let cy = from.y;

    for (let i = 0; i < speed; i++) {
      const nx = cx + step.x;
      const ny = cy + step.y;

      if (!this.isWalkable(vec2(nx, ny))) break;

      cx = nx;
      cy = ny;
    }
    return vec2(cx, cy);
  }

  // ── Grid Modifications ──

  destroyWall(pos: Vec2): void {
    if (this.isInBounds(pos) && this._grid[pos.x][pos.y] === TileType.DestructibleWall) {
      this._grid[pos.x][pos.y] = TileType.Empty;
    }
  }

  placeBarrier(pos: Vec2): void {
    if (this.isInBounds(pos) && this._grid[pos.x][pos.y] === TileType.Empty) {
      this._grid[pos.x][pos.y] = TileType.Wall;
    }
  }

  removeBarrier(pos: Vec2): void {
    if (this.isInBounds(pos) && this._grid[pos.x][pos.y] === TileType.Wall) {
      this._grid[pos.x][pos.y] = TileType.Empty;
    }
  }

  expandDangerZone(tiles: Vec2[]): void {
    for (const tile of tiles) {
      if (!this.isInBounds(tile)) continue;
      this._dangerZone.add(this._key(tile));
      if (this._grid[tile.x][tile.y] === TileType.DestructibleWall) {
        this._grid[tile.x][tile.y] = TileType.Empty;
      }
    }
  }

  // ── Direction Helpers (Static) ──

  static directionToVector(dir: Direction): Vec2 {
    switch (dir) {
      case Direction.Up:    return vec2(0, 1);
      case Direction.Down:  return vec2(0, -1);
      case Direction.Left:  return vec2(-1, 0);
      case Direction.Right: return vec2(1, 0);
      default:              return vec2(0, 0);
    }
  }

  static turnLeft(dir: Direction): Direction {
    switch (dir) {
      case Direction.Up:    return Direction.Left;
      case Direction.Left:  return Direction.Down;
      case Direction.Down:  return Direction.Right;
      case Direction.Right: return Direction.Up;
      default:              return dir;
    }
  }

  static turnRight(dir: Direction): Direction {
    switch (dir) {
      case Direction.Up:    return Direction.Right;
      case Direction.Right: return Direction.Down;
      case Direction.Down:  return Direction.Left;
      case Direction.Left:  return Direction.Up;
      default:              return dir;
    }
  }

  static turnAround(dir: Direction): Direction {
    switch (dir) {
      case Direction.Up:    return Direction.Down;
      case Direction.Down:  return Direction.Up;
      case Direction.Left:  return Direction.Right;
      case Direction.Right: return Direction.Left;
      default:              return dir;
    }
  }

  static vectorToDirection(v: Vec2): Direction {
    if (v.y > 0) return Direction.Up;
    if (v.y < 0) return Direction.Down;
    if (v.x < 0) return Direction.Left;
    if (v.x > 0) return Direction.Right;
    return Direction.Up;
  }

  // ── Internal ──

  private _key(pos: Vec2): string {
    return `${pos.x},${pos.y}`;
  }
}
