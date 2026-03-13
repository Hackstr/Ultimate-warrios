import { Injectable, Logger } from '@nestjs/common';
import * as crypto from 'crypto';
import { PrismaService } from '../prisma/prisma.service';
import { ActionResolverService } from './action-resolver.service';
import { ActionType, Direction, MatchResult, RoundResult, TileType } from '../shared/models/enums';
import {
  StepResult,
  MapConfig,
  HeroConfig,
  Vec2,
  vec2,
} from '../shared/models/game-types';
import {
  MatchFoundPayload,
  StepResultPayload,
} from '../shared/models/dto';

// ── In-memory active match state ──

interface ActiveMatch {
  matchId: string;
  roomId: string;
  player1Id: string;
  player2Id: string;
  player1HeroId: string;
  player2HeroId: string;
  map: MapConfig;
  currentRound: number;
  /** Per-round commit state */
  p1Commit: string | null;
  p2Commit: string | null;
  p1Reveal: { actions: ActionType[]; nonce: string } | null;
  p2Reveal: { actions: ActionType[]; nonce: string } | null;
  /** Persistent resolver across rounds */
  resolver: ActionResolverService;
  roundResults: RoundResult[];
}

interface CreateMatchResult {
  roomId: string;
  payloadForPlayer1: MatchFoundPayload;
  payloadForPlayer2: MatchFoundPayload;
}

interface CommitResult {
  success: boolean;
  error?: string;
  bothCommitted: boolean;
}

interface RevealResult {
  success: boolean;
  error?: string;
  roundResolved: boolean;
  steps?: StepResultPayload[];
  roundOutcome?: number;
  matchEnded?: boolean;
  matchOutcome?: number;
  nextRound?: number;
  shrinkZone?: { x: number; y: number }[];
}

@Injectable()
export class MatchService {
  private readonly _logger = new Logger(MatchService.name);

  /** playerId → ActiveMatch */
  private readonly _playerMatches = new Map<string, ActiveMatch>();
  /** matchId → ActiveMatch */
  private readonly _matches = new Map<string, ActiveMatch>();

  constructor(
    private readonly _prisma: PrismaService,
  ) {}

  // ── Match Creation ──

  async createMatch(
    p1Id: string,
    p1HeroId: string,
    p2Id: string,
    p2HeroId: string,
  ): Promise<CreateMatchResult> {
    const map = this._getDefaultMap();
    const p1Config = this._getHeroConfig(p1HeroId);
    const p2Config = this._getHeroConfig(p2HeroId);

    const dbMatch = await this._prisma.match.create({
      data: {
        player1Id: p1Id,
        player1Hero: p1HeroId,
        player2Id: p2Id,
        player2Hero: p2HeroId,
        mapId: map.mapId,
      },
    });

    const resolver = new ActionResolverService(
      map,
      p1Config,
      p2Config,
      map.spawnPoints[0],
      map.spawnFacings[0],
      map.spawnPoints[1],
      map.spawnFacings[1],
    );

    const active: ActiveMatch = {
      matchId: dbMatch.id,
      roomId: `match:${dbMatch.id}`,
      player1Id: p1Id,
      player2Id: p2Id,
      player1HeroId: p1HeroId,
      player2HeroId: p2HeroId,
      map,
      currentRound: 1,
      p1Commit: null,
      p2Commit: null,
      p1Reveal: null,
      p2Reveal: null,
      resolver,
      roundResults: [],
    };

    this._matches.set(dbMatch.id, active);
    this._playerMatches.set(p1Id, active);
    this._playerMatches.set(p2Id, active);

    return {
      roomId: active.roomId,
      payloadForPlayer1: this._buildMatchPayload(active, true),
      payloadForPlayer2: this._buildMatchPayload(active, false),
    };
  }

  // ── Commit-Reveal ──

  async submitCommit(playerId: string, hash: string): Promise<CommitResult> {
    const match = this._playerMatches.get(playerId);
    if (!match) return { success: false, error: 'No active match', bothCommitted: false };

    if (playerId === match.player1Id) {
      if (match.p1Commit) return { success: false, error: 'Already committed', bothCommitted: false };
      match.p1Commit = hash;
    } else {
      if (match.p2Commit) return { success: false, error: 'Already committed', bothCommitted: false };
      match.p2Commit = hash;
    }

    const both = match.p1Commit !== null && match.p2Commit !== null;
    return { success: true, bothCommitted: both };
  }

  async submitReveal(
    playerId: string,
    actions: ActionType[],
    nonce: string,
  ): Promise<RevealResult> {
    const match = this._playerMatches.get(playerId);
    if (!match) return { success: false, error: 'No active match', roundResolved: false };

    const expectedHash = playerId === match.player1Id ? match.p1Commit : match.p2Commit;
    if (!expectedHash) return { success: false, error: 'No commit found', roundResolved: false };

    if (!this._verifyHash(actions, nonce, expectedHash)) {
      return { success: false, error: 'Hash mismatch — commit does not match reveal', roundResolved: false };
    }

    if (playerId === match.player1Id) {
      match.p1Reveal = { actions, nonce };
    } else {
      match.p2Reveal = { actions, nonce };
    }

    if (!match.p1Reveal || !match.p2Reveal) {
      return { success: true, roundResolved: false };
    }

    // Both revealed — resolve the round
    return this._resolveRound(match);
  }

  // ── Surrender ──

  async surrender(playerId: string): Promise<{ winner: number } | null> {
    const match = this._playerMatches.get(playerId);
    if (!match) return null;

    const winner = playerId === match.player1Id
      ? MatchResult.Player2Win
      : MatchResult.Player1Win;

    await this._endMatch(match, winner);
    return { winner };
  }

  // ── Disconnect ──

  handleDisconnect(playerId: string): void {
    const match = this._playerMatches.get(playerId);
    if (!match) return;
    this._logger.warn(`Player ${playerId} disconnected from match ${match.matchId}`);
  }

  // ── Queries ──

  getRoomId(playerId: string): string | null {
    return this._playerMatches.get(playerId)?.roomId ?? null;
  }

  // ── Round Resolution ──

  private async _resolveRound(match: ActiveMatch): Promise<RevealResult> {
    const p1Actions = match.p1Reveal!.actions;
    const p2Actions = match.p2Reveal!.actions;

    const steps = match.resolver.resolveRound(p1Actions, p2Actions);
    const stepsPayload = steps.map(this._stepToPayload);

    const roundOutcome = this._determineRoundOutcome(steps);
    match.roundResults.push(roundOutcome);

    await this._prisma.round.create({
      data: {
        matchId: match.matchId,
        roundNumber: match.currentRound,
        outcome: this._roundOutcomeToDb(roundOutcome),
        p1Actions: p1Actions.map(Number),
        p2Actions: p2Actions.map(Number),
        p1CommitHash: match.p1Commit,
        p2CommitHash: match.p2Commit,
        p1Nonce: match.p1Reveal!.nonce,
        p2Nonce: match.p2Reveal!.nonce,
        stepsJson: JSON.stringify(stepsPayload),
      },
    });

    const matchEnded = this._isMatchOver(match);

    if (matchEnded) {
      const matchOutcome = this._determineMatchOutcome(match);
      await this._endMatch(match, matchOutcome);

      return {
        success: true,
        roundResolved: true,
        steps: stepsPayload,
        roundOutcome,
        matchEnded: true,
        matchOutcome,
      };
    }

    // Prepare next round
    this._resetRoundState(match);
    match.currentRound++;
    match.resolver.resetForNewRound();

    return {
      success: true,
      roundResolved: true,
      steps: stepsPayload,
      roundOutcome,
      matchEnded: false,
      nextRound: match.currentRound,
      shrinkZone: [],
    };
  }

  // ── Hash Verification ──

  private _verifyHash(actions: ActionType[], nonce: string, expectedHash: string): boolean {
    const actionsJson = JSON.stringify(actions);
    const input = actionsJson + nonce;
    const computed = crypto.createHash('sha256').update(input).digest('hex');
    return computed === expectedHash;
  }

  // ── Outcome Logic ──

  private _determineRoundOutcome(steps: StepResult[]): RoundResult {
    for (const step of steps) {
      if (step.p1Eliminated && step.p2Eliminated) return RoundResult.MutualCancel;
      if (step.p1Eliminated) return RoundResult.Player2Kill;
      if (step.p2Eliminated) return RoundResult.Player1Kill;
    }
    return RoundResult.NoKill;
  }

  private _isMatchOver(match: ActiveMatch): boolean {
    const lastRound = match.roundResults[match.roundResults.length - 1];
    return lastRound === RoundResult.Player1Kill || lastRound === RoundResult.Player2Kill;
  }

  private _determineMatchOutcome(match: ActiveMatch): MatchResult {
    const lastRound = match.roundResults[match.roundResults.length - 1];
    if (lastRound === RoundResult.Player1Kill) return MatchResult.Player1Win;
    return MatchResult.Player2Win;
  }

  // ── DB Persistence ──

  private async _endMatch(match: ActiveMatch, outcome: MatchResult): Promise<void> {
    const dbOutcome = outcome === MatchResult.Player1Win
      ? 'PLAYER1_WIN'
      : outcome === MatchResult.Player2Win
        ? 'PLAYER2_WIN'
        : 'DRAW';

    await this._prisma.match.update({
      where: { id: match.matchId },
      data: { status: 'COMPLETED', outcome: dbOutcome, endedAt: new Date() },
    });

    await this._updatePlayerStats(match, outcome);
    this._cleanupMatch(match);
  }

  private async _updatePlayerStats(match: ActiveMatch, outcome: MatchResult): Promise<void> {
    const K = 32;
    const p1 = await this._prisma.player.findUnique({ where: { id: match.player1Id } });
    const p2 = await this._prisma.player.findUnique({ where: { id: match.player2Id } });
    if (!p1 || !p2) return;

    const expected1 = 1 / (1 + Math.pow(10, (p2.rating - p1.rating) / 400));
    const expected2 = 1 - expected1;

    let score1: number;
    let score2: number;
    if (outcome === MatchResult.Player1Win) { score1 = 1; score2 = 0; }
    else if (outcome === MatchResult.Player2Win) { score1 = 0; score2 = 1; }
    else { score1 = 0.5; score2 = 0.5; }

    const newRating1 = Math.round(p1.rating + K * (score1 - expected1));
    const newRating2 = Math.round(p2.rating + K * (score2 - expected2));

    await this._prisma.player.update({
      where: { id: p1.id },
      data: {
        rating: newRating1,
        wins: { increment: score1 === 1 ? 1 : 0 },
        losses: { increment: score1 === 0 ? 1 : 0 },
        draws: { increment: score1 === 0.5 ? 1 : 0 },
      },
    });
    await this._prisma.player.update({
      where: { id: p2.id },
      data: {
        rating: newRating2,
        wins: { increment: score2 === 1 ? 1 : 0 },
        losses: { increment: score2 === 0 ? 1 : 0 },
        draws: { increment: score2 === 0.5 ? 1 : 0 },
      },
    });
  }

  // ── State Management ──

  private _resetRoundState(match: ActiveMatch): void {
    match.p1Commit = null;
    match.p2Commit = null;
    match.p1Reveal = null;
    match.p2Reveal = null;
  }

  private _cleanupMatch(match: ActiveMatch): void {
    this._matches.delete(match.matchId);
    this._playerMatches.delete(match.player1Id);
    this._playerMatches.delete(match.player2Id);
  }

  // ── Payload Builders ──

  private _buildMatchPayload(match: ActiveMatch, isPlayer1: boolean): MatchFoundPayload {
    return {
      matchId: match.matchId,
      opponentName: isPlayer1 ? match.player2Id : match.player1Id,
      opponentHeroId: isPlayer1 ? match.player2HeroId : match.player1HeroId,
      mapId: match.map.mapId,
      mapWidth: match.map.width,
      mapHeight: match.map.height,
      gridData: match.map.gridData.map((row) => row.map(Number)),
      yourSpawn: match.map.spawnPoints[isPlayer1 ? 0 : 1],
      opponentSpawn: match.map.spawnPoints[isPlayer1 ? 1 : 0],
      yourFacing: match.map.spawnFacings[isPlayer1 ? 0 : 1],
      opponentFacing: match.map.spawnFacings[isPlayer1 ? 1 : 0],
    };
  }

  private _stepToPayload(step: StepResult): StepResultPayload {
    return {
      stepIndex: step.stepIndex,
      p1Action: step.p1Action,
      p1StartPos: step.p1StartPos,
      p1EndPos: step.p1EndPos,
      p1StartFacing: step.p1StartFacing,
      p1EndFacing: step.p1EndFacing,
      p2Action: step.p2Action,
      p2StartPos: step.p2StartPos,
      p2EndPos: step.p2EndPos,
      p2StartFacing: step.p2StartFacing,
      p2EndFacing: step.p2EndFacing,
      p1Fired: step.p1Fired,
      p2Fired: step.p2Fired,
      p1Hit: step.p1Hit,
      p2Hit: step.p2Hit,
      mutualCancel: step.mutualCancel,
      p1ArmorBroken: step.p1ArmorBroken,
      p2ArmorBroken: step.p2ArmorBroken,
      p1Eliminated: step.p1Eliminated,
      p2Eliminated: step.p2Eliminated,
      p1PickedUp: step.p1PickedUp,
      p2PickedUp: step.p2PickedUp,
    };
  }

  // ── Game Data (hardcoded for MVP, should move to config) ──

  private _roundOutcomeToDb(r: RoundResult): 'PLAYER1_KILL' | 'PLAYER2_KILL' | 'MUTUAL_CANCEL' | 'NO_KILL' {
    switch (r) {
      case RoundResult.Player1Kill: return 'PLAYER1_KILL';
      case RoundResult.Player2Kill: return 'PLAYER2_KILL';
      case RoundResult.MutualCancel: return 'MUTUAL_CANCEL';
      case RoundResult.NoKill: return 'NO_KILL';
    }
  }

  private _getDefaultMap(): MapConfig {
    const W = 10;
    const H = 10;
    // Column-major: gridData[x][y] matches C# _grid[col, row]
    const grid: TileType[][] = [];
    for (let x = 0; x < W; x++) {
      const col: TileType[] = [];
      for (let y = 0; y < H; y++) col.push(TileType.Empty);
      grid.push(col);
    }
    return {
      mapId: 'arena_01',
      width: W,
      height: H,
      gridData: grid,
      pickups: [],
      spawnPoints: [vec2(1, 1), vec2(8, 8)],
      spawnFacings: [Direction.Up, Direction.Left],
    };
  }

  private _getHeroConfig(heroId: string): HeroConfig {
    const heroes: Record<string, HeroConfig> = {
      archer:    { heroId: 'archer',    heroName: 'Archer',    steps: 4, range: 8,  cooldown: 2, armor: 0, speed: 1, specialName: 'Ricochet' },
      tank:      { heroId: 'tank',      heroName: 'Tank',      steps: 4, range: 4,  cooldown: 1, armor: 1, speed: 1, specialName: 'Push' },
      shadow:    { heroId: 'shadow',    heroName: 'Shadow',    steps: 6, range: 3,  cooldown: 1, armor: 0, speed: 2, specialName: 'Blink' },
      scout:     { heroId: 'scout',     heroName: 'Scout',     steps: 5, range: 5,  cooldown: 1, armor: 0, speed: 2, specialName: 'Scan' },
      mage:      { heroId: 'mage',      heroName: 'Mage',      steps: 4, range: 6,  cooldown: 2, armor: 0, speed: 1, specialName: 'PhaseShot' },
      demo:      { heroId: 'demo',      heroName: 'Demo',      steps: 4, range: 5,  cooldown: 2, armor: 0, speed: 1, specialName: 'Bomb' },
      guardian:  { heroId: 'guardian',   heroName: 'Guardian',  steps: 4, range: 5,  cooldown: 2, armor: 1, speed: 1, specialName: 'Barrier' },
      ghost:     { heroId: 'ghost',     heroName: 'Ghost',     steps: 5, range: 4,  cooldown: 1, armor: 0, speed: 1, specialName: 'Cloak' },
      engineer:  { heroId: 'engineer',  heroName: 'Engineer',  steps: 4, range: 5,  cooldown: 2, armor: 0, speed: 1, specialName: 'Turret' },
      berserker: { heroId: 'berserker', heroName: 'Berserker', steps: 6, range: 2,  cooldown: 0, armor: 0, speed: 1, specialName: 'Charge' },
      hawk:      { heroId: 'hawk',      heroName: 'Hawk',      steps: 3, range: 10, cooldown: 3, armor: 0, speed: 1, specialName: 'Pierce' },
      mirage:    { heroId: 'mirage',    heroName: 'Mirage',    steps: 5, range: 4,  cooldown: 1, armor: 0, speed: 1, specialName: 'Decoy' },
    };
    return heroes[heroId] ?? heroes['archer'];
  }
}
