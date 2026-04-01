/**
 * Exact mirror of C# TacticalDuelist.Core.Models enums.
 * Any change here MUST be reflected in the Unity client.
 */

export enum Direction {
  Up = 0,
  Down = 1,
  Left = 2,
  Right = 3,
}

export enum ActionType {
  Move = 0,
  TurnLeft = 1,
  TurnRight = 2,
  TurnAround = 3,
  Shoot = 4,
  Wait = 5,
  Special = 6,
  Shield = 7,
}

export enum GamePhase {
  Matchmaking = 0,
  HeroSelect = 1,
  Planning = 2,
  Execution = 3,
  PostRound = 4,
  PostMatch = 5,
}

export enum MatchResult {
  Player1Win = 0,
  Player2Win = 1,
  Draw = 2,
}

export enum RoundResult {
  Player1Kill = 0,
  Player2Kill = 1,
  MutualCancel = 2,
  NoKill = 3,
}

export enum TileType {
  Empty = 0,
  Wall = 1,
  DestructibleWall = 2,
  DangerZone = 3,
  OutOfBounds = 4,
}

export enum PickupType {
  ArmorShard = 0,
  IntelOrb = 1,
  SpeedBoost = 2,
  RangeBoost = 3,
}

export enum SpecialAbility {
  Ricochet = 0,
  Push = 1,
  Blink = 2,
  Scan = 3,
  PhaseShot = 4,
  Bomb = 5,
  Barrier = 6,
  Cloak = 7,
  Turret = 8,
  Charge = 9,
  Pierce = 10,
  Decoy = 11,
}
