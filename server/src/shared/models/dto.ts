import { IsArray, IsEnum, IsInt, IsOptional, IsString, Min } from 'class-validator';
import { ActionType } from './enums';

// ── Client → Server DTOs ──

export class FindMatchDto {
  @IsString()
  heroId!: string;

  @IsInt()
  @Min(0)
  rankTier!: number;

  @IsOptional()
  @IsInt()
  @Min(0)
  stakeLevel?: number;
}

export class CancelMatchDto {}

export class RoundCommitDto {
  @IsString()
  hash!: string;
}

export class RoundRevealDto {
  @IsArray()
  @IsEnum(ActionType, { each: true })
  actions!: ActionType[];

  @IsString()
  nonce!: string;
}

export class SurrenderDto {}

// ── Server → Client Payloads ──

export interface MatchFoundPayload {
  matchId: string;
  opponentName: string;
  opponentHeroId: string;
  mapId: string;
  mapWidth: number;
  mapHeight: number;
  gridData: number[][];
  yourSpawn: { x: number; y: number };
  opponentSpawn: { x: number; y: number };
  yourFacing: number;
  opponentFacing: number;
}

export interface MatchErrorPayload {
  code: string;
  message: string;
}

export interface RoundStartPayload {
  roundNumber: number;
  timeLimit: number;
  shrinkZone?: { x: number; y: number }[];
}

export interface RoundResultsPayload {
  steps: StepResultPayload[];
}

export interface StepResultPayload {
  stepIndex: number;
  p1Action: number;
  p1StartPos: { x: number; y: number };
  p1EndPos: { x: number; y: number };
  p1StartFacing: number;
  p1EndFacing: number;
  p2Action: number;
  p2StartPos: { x: number; y: number };
  p2EndPos: { x: number; y: number };
  p2StartFacing: number;
  p2EndFacing: number;
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
  p1PickedUp: number | null;
  p2PickedUp: number | null;
}

export interface RoundEndPayload {
  result: number;
}

export interface MatchEndPayload {
  winner: number;
  rewards?: unknown;
}
