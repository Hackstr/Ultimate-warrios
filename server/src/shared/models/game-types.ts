/**
 * Core game data types — mirrors Unity C# models.
 * Uses plain objects instead of Unity Vector2Int.
 */

import {
  ActionType,
  Direction,
  PickupType,
  TileType,
} from './enums';

// ── Geometry ──

export interface Vec2 {
  readonly x: number;
  readonly y: number;
}

export function vec2(x: number, y: number): Vec2 {
  return { x, y };
}

export function vec2Eq(a: Vec2, b: Vec2): boolean {
  return a.x === b.x && a.y === b.y;
}

export function vec2Add(a: Vec2, b: Vec2): Vec2 {
  return { x: a.x + b.x, y: a.y + b.y };
}

// ── Hero Config (mirrors ScriptableObject) ──

export interface HeroConfig {
  heroId: string;
  heroName: string;
  steps: number;
  range: number;
  cooldown: number;
  armor: number;
  speed: number;
  specialName: string;
}

// ── Hero Runtime State ──

export interface HeroState {
  config: HeroConfig;
  playerIndex: number;
  position: Vec2;
  facing: Direction;
  isAlive: boolean;
  hasArmor: boolean;
  cooldownRemaining: number;
  specialUsedThisRound: boolean;
  bonusSpeed: number;
  bonusRange: number;
  hasIntel: boolean;
  isCloaked: boolean;
  cloakStepsRemaining: number;
}

export function createHeroState(
  config: HeroConfig,
  playerIndex: number,
  spawnPos: Vec2,
  spawnFacing: Direction,
): HeroState {
  return {
    config,
    playerIndex,
    position: { ...spawnPos },
    facing: spawnFacing,
    isAlive: true,
    hasArmor: config.armor > 0,
    cooldownRemaining: 0,
    specialUsedThisRound: false,
    bonusSpeed: 0,
    bonusRange: 0,
    hasIntel: false,
    isCloaked: false,
    cloakStepsRemaining: 0,
  };
}

export function heroEffectiveSpeed(h: HeroState): number {
  return h.config.speed + h.bonusSpeed;
}

export function heroEffectiveRange(h: HeroState): number {
  return h.config.range + h.bonusRange;
}

export function resetHeroForNewRound(h: HeroState): void {
  h.cooldownRemaining = 0;
  h.specialUsedThisRound = false;
  h.bonusSpeed = 0;
  h.bonusRange = 0;
  h.hasIntel = false;
  h.isCloaked = false;
  h.cloakStepsRemaining = 0;
}

// ── Step Result (mirrors C# StepResult) ──

export interface StepResult {
  stepIndex: number;

  p1Action: ActionType;
  p1StartPos: Vec2;
  p1EndPos: Vec2;
  p1StartFacing: Direction;
  p1EndFacing: Direction;

  p2Action: ActionType;
  p2StartPos: Vec2;
  p2EndPos: Vec2;
  p2StartFacing: Direction;
  p2EndFacing: Direction;

  p1Fired: boolean;
  p2Fired: boolean;
  p1Hit: boolean;
  p2Hit: boolean;
  mutualCancel: boolean;
  p1ArmorBroken: boolean;
  p2ArmorBroken: boolean;
  p1Eliminated: boolean;
  p2Eliminated: boolean;
  p1Shielded: boolean;
  p2Shielded: boolean;

  p1PickedUp: PickupType | null;
  p2PickedUp: PickupType | null;
}

export function createEmptyStepResult(stepIndex: number): StepResult {
  return {
    stepIndex,
    p1Action: ActionType.Wait,
    p1StartPos: vec2(0, 0),
    p1EndPos: vec2(0, 0),
    p1StartFacing: Direction.Up,
    p1EndFacing: Direction.Up,
    p2Action: ActionType.Wait,
    p2StartPos: vec2(0, 0),
    p2EndPos: vec2(0, 0),
    p2StartFacing: Direction.Up,
    p2EndFacing: Direction.Up,
    p1Fired: false,
    p2Fired: false,
    p1Hit: false,
    p2Hit: false,
    mutualCancel: false,
    p1ArmorBroken: false,
    p2ArmorBroken: false,
    p1Eliminated: false,
    p2Eliminated: false,
    p1Shielded: false,
    p2Shielded: false,
    p1PickedUp: null,
    p2PickedUp: null,
  };
}

// ── Map Config ──

export interface PickupPlacement {
  position: Vec2;
  type: PickupType;
}

export interface MapConfig {
  mapId: string;
  width: number;
  height: number;
  gridData: TileType[][];
  pickups: PickupPlacement[];
  spawnPoints: Vec2[];
  spawnFacings: Direction[];
}
