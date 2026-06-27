import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import * as crypto from 'crypto';
import { PrismaService } from '../prisma/prisma.service';
import { PlayerService } from '../player/player.service';
import { BlockchainService } from '../blockchain/blockchain.service';
import { RedisService } from '../shared/services/redis.service';
import { ActionResolverService } from './action-resolver.service';
import { ActionType, Direction, MatchResult, RoundResult, TileType } from '../shared/models/enums';
import {
  StepResult,
  MapConfig,
  HeroConfig,
  HeroState,
  Vec2,
  vec2,
} from '../shared/models/game-types';
import {
  MatchFoundPayload,
  StepResultPayload,
} from '../shared/models/dto';
import { Prisma } from '@prisma/client';
import { getHeroConfig, VALID_HERO_IDS } from '../shared/config/hero-configs';

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
  player1Name: string;
  player2Name: string;
  /** Disconnect grace period tracking */
  disconnectTimers: Map<string, ReturnType<typeof setTimeout>>;
  /** Which phase the match is in for reconnection */
  phase: 'planning' | 'committed' | 'resolving';
  /** Guard against double _endMatch calls */
  isEnded: boolean;
  /** Reveal timeout — ends match if reveal not received in time */
  revealTimer: ReturnType<typeof setTimeout> | null;
  /** Stake amount in lamports (0 = free match) */
  stakeLevel: number;
}

interface CreateMatchResult {
  roomId: string;
  payloadForPlayer1: MatchFoundPayload;
  payloadForPlayer2: MatchFoundPayload;
}

interface RejoinResult {
  matchId: string;
  roomId: string;
  currentRound: number;
  phase: 'planning' | 'committed' | 'resolving';
  yourHeroId: string;
  opponentHeroId: string;
  opponentId: string;
  mapId: string;
  mapWidth: number;
  mapHeight: number;
  gridData: number[][];
  yourPos: { x: number; y: number };
  opponentPos: { x: number; y: number };
  yourFacing: number;
  opponentFacing: number;
  yourAlive: boolean;
  opponentAlive: boolean;
  yourArmor: boolean;
  opponentArmor: boolean;
  hasCommitted: boolean;
  timeLimit: number;
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
  /** Per-player match-end data (only when matchEnded=true) */
  p1RatingDelta?: number;
  p2RatingDelta?: number;
  p1Coins?: number;
  p2Coins?: number;
  player1Id?: string;
  player2Id?: string;
}

/** Serializable snapshot of an active match for Redis checkpointing. */
interface MatchSnapshot {
  matchId: string;
  roomId: string;
  player1Id: string;
  player2Id: string;
  player1Name: string;
  player2Name: string;
  player1HeroId: string;
  player2HeroId: string;
  map: MapConfig;
  currentRound: number;
  roundResults: RoundResult[];
  phase: 'planning' | 'committed' | 'resolving';
  stakeLevel: number;
  heroStates: { p1: HeroState; p2: HeroState };
}

@Injectable()
export class MatchService implements OnModuleInit {
  private readonly _logger = new Logger(MatchService.name);
  private static readonly REDIS_KEY_PREFIX = 'match:active:';
  private static readonly REDIS_TTL = 3600; // 1 hour

  /** playerId → ActiveMatch */
  private readonly _playerMatches = new Map<string, ActiveMatch>();
  /** matchId → ActiveMatch */
  private readonly _matches = new Map<string, ActiveMatch>();
  /** Tracks matches currently being ended to prevent concurrent _endMatch calls */
  private readonly _endingMatches = new Set<string>();

  constructor(
    private readonly _prisma: PrismaService,
    private readonly _players: PlayerService,
    private readonly _blockchain: BlockchainService,
    private readonly _redis: RedisService,
  ) {}

  async onModuleInit() {
    await this._restoreMatchesFromRedis();
  }

  // ── Match Creation ──

  async createMatch(
    p1Id: string,
    p1HeroId: string,
    p2Id: string,
    p2HeroId: string,
  ): Promise<CreateMatchResult> {
    const map = this._getRandomMap();
    const p1Config = this._getHeroConfig(p1HeroId);
    const p2Config = this._getHeroConfig(p2HeroId);

    const [dbMatch, p1Info, p2Info] = await Promise.all([
      this._prisma.match.create({
        data: {
          player1Id: p1Id,
          player1Hero: p1HeroId,
          player2Id: p2Id,
          player2Hero: p2HeroId,
          mapId: map.mapId,
        },
      }),
      this._prisma.player.findUnique({ where: { id: p1Id }, select: { displayName: true } }),
      this._prisma.player.findUnique({ where: { id: p2Id }, select: { displayName: true } }),
    ]);

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
      player1Name: p1Info?.displayName ?? 'Duelist',
      player2Name: p2Info?.displayName ?? 'Duelist',
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
      disconnectTimers: new Map(),
      phase: 'planning',
      isEnded: false,
      revealTimer: null,
      stakeLevel: 0, // Set by gateway when staked match
    };

    this._matches.set(dbMatch.id, active);
    this._playerMatches.set(p1Id, active);
    this._playerMatches.set(p2Id, active);

    await this._checkpointMatch(active);

    return {
      roomId: active.roomId,
      payloadForPlayer1: this._buildMatchPayload(active, true),
      payloadForPlayer2: this._buildMatchPayload(active, false),
    };
  }

  // ── Commit-Reveal ──

  async submitCommit(playerId: string, hash: string): Promise<CommitResult> {
    const match = this._playerMatches.get(playerId);
    if (!match || match.isEnded) return { success: false, error: 'No active match', bothCommitted: false };

    if (playerId === match.player1Id) {
      if (match.p1Commit) return { success: false, error: 'Already committed', bothCommitted: false };
      match.p1Commit = hash;
    } else {
      if (match.p2Commit) return { success: false, error: 'Already committed', bothCommitted: false };
      match.p2Commit = hash;
    }

    const both = match.p1Commit !== null && match.p2Commit !== null;
    if (both) {
      match.phase = 'committed';
      // Start 40s reveal timeout — if neither reveals, end match as draw
      if (match.revealTimer) clearTimeout(match.revealTimer);
      match.revealTimer = setTimeout(async () => {
        try {
          if (!match.isEnded && match.phase === 'committed') {
            this._logger.warn(`Reveal timeout for match ${match.matchId} — ending as draw`);
            await this._endMatch(match, MatchResult.Draw);
            this._onForfeit?.(match.matchId, match.player1Id, MatchResult.Draw);
            this._onForfeit?.(match.matchId, match.player2Id, MatchResult.Draw);
          }
        } catch (err) {
          this._logger.error(`Reveal timeout error: ${err}`);
        }
      }, 40_000);
    }
    return { success: true, bothCommitted: both };
  }

  async submitReveal(
    playerId: string,
    actions: ActionType[],
    nonce: string,
  ): Promise<RevealResult> {
    const match = this._playerMatches.get(playerId);
    if (!match || match.isEnded) return { success: false, error: 'No active match', roundResolved: false };

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

    // Both revealed — cancel reveal timeout and resolve
    if (match.revealTimer) { clearTimeout(match.revealTimer); match.revealTimer = null; }
    return this._resolveRound(match);
  }

  // ── Surrender ──

  async surrender(playerId: string): Promise<{ winner: number } | null> {
    const match = this._playerMatches.get(playerId);
    if (!match || match.isEnded) return null;

    const winner = playerId === match.player1Id
      ? MatchResult.Player2Win
      : MatchResult.Player1Win;

    await this._endMatch(match, winner);
    return { winner };
  }

  // ── Disconnect / Reconnect ──

  private static readonly DISCONNECT_GRACE_SEC = 60;

  handleDisconnect(playerId: string): { matchId: string; opponentId: string } | null {
    const match = this._playerMatches.get(playerId);
    if (!match) return null;

    this._logger.warn(`Player ${playerId} disconnected from match ${match.matchId} — starting ${MatchService.DISCONNECT_GRACE_SEC}s grace period`);

    // Clear any existing timer for this player (e.g. rapid re-disconnect)
    const existing = match.disconnectTimers.get(playerId);
    if (existing) clearTimeout(existing);

    const opponentId = playerId === match.player1Id ? match.player2Id : match.player1Id;

    const timer = setTimeout(async () => {
      try {
        match.disconnectTimers.delete(playerId);
        this._logger.warn(`Grace period expired for ${playerId} — forfeit match ${match.matchId}`);

        const winner = playerId === match.player1Id
          ? MatchResult.Player2Win
          : MatchResult.Player1Win;
        await this._endMatch(match, winner);

        // Notify via callback (gateway will emit match:end to opponent)
        this._onForfeit?.(match.matchId, opponentId, winner);
      } catch (err) {
        this._logger.error(`Forfeit timer error for match ${match.matchId}`, err);
        // Still try to notify opponent even if _endMatch failed
        this._onForfeit?.(match.matchId, opponentId,
          playerId === match.player1Id ? MatchResult.Player2Win : MatchResult.Player1Win);
      }
    }, MatchService.DISCONNECT_GRACE_SEC * 1000);

    match.disconnectTimers.set(playerId, timer);

    return { matchId: match.matchId, opponentId };
  }

  /** Callback set by gateway to notify opponent of forfeit after grace period. */
  private _onForfeit?: (matchId: string, opponentId: string, winner: number) => void;

  setForfeitCallback(cb: (matchId: string, opponentId: string, winner: number) => void): void {
    this._onForfeit = cb;
  }

  /**
   * Rejoin a match after reconnect. Cancels the grace timer and returns
   * full match state so the client can restore.
   */
  rejoinMatch(playerId: string): RejoinResult | null {
    const match = this._playerMatches.get(playerId);
    if (!match || match.isEnded) return null;

    // Cancel disconnect timer
    const timer = match.disconnectTimers.get(playerId);
    if (timer) {
      clearTimeout(timer);
      match.disconnectTimers.delete(playerId);
      this._logger.log(`Player ${playerId} rejoined match ${match.matchId} — grace timer cancelled`);
    }

    const isP1 = playerId === match.player1Id;
    const heroes = match.resolver.getHeroStates();
    const myHero = isP1 ? heroes.p1 : heroes.p2;
    const oppHero = isP1 ? heroes.p2 : heroes.p1;

    return {
      matchId: match.matchId,
      roomId: match.roomId,
      currentRound: match.currentRound,
      phase: match.phase,
      yourHeroId: isP1 ? match.player1HeroId : match.player2HeroId,
      opponentHeroId: isP1 ? match.player2HeroId : match.player1HeroId,
      opponentId: isP1 ? match.player2Id : match.player1Id,
      mapId: match.map.mapId,
      mapWidth: match.map.width,
      mapHeight: match.map.height,
      gridData: match.map.gridData.map((row) => row.map(Number)),
      yourPos: myHero.position,
      opponentPos: oppHero.position,
      yourFacing: myHero.facing,
      opponentFacing: oppHero.facing,
      yourAlive: myHero.isAlive,
      opponentAlive: oppHero.isAlive,
      yourArmor: myHero.hasArmor,
      opponentArmor: oppHero.hasArmor,
      hasCommitted: isP1 ? match.p1Commit !== null : match.p2Commit !== null,
      timeLimit: 30,
    };
  }

  /**
   * Find the active match for a player (used by gateway for rejoin).
   */
  getActiveMatchId(playerId: string): string | null {
    return this._playerMatches.get(playerId)?.matchId ?? null;
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
      const p1Id = match.player1Id;
      const p2Id = match.player2Id;
      const endData = await this._endMatch(match, matchOutcome);

      return {
        success: true,
        roundResolved: true,
        steps: stepsPayload,
        roundOutcome,
        matchEnded: true,
        matchOutcome,
        p1RatingDelta: endData.p1RatingDelta,
        p2RatingDelta: endData.p2RatingDelta,
        p1Coins: endData.p1Coins,
        p2Coins: endData.p2Coins,
        player1Id: p1Id,
        player2Id: p2Id,
      };
    }

    // Prepare next round
    this._resetRoundState(match);
    match.currentRound++;
    match.resolver.resetForNewRound();

    await this._checkpointMatch(match);

    return {
      success: true,
      roundResolved: true,
      steps: stepsPayload,
      roundOutcome,
      matchEnded: false,
      nextRound: match.currentRound,
      shrinkZone: this._calculateShrinkZone(match),
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
    if (lastRound === RoundResult.Player1Kill || lastRound === RoundResult.Player2Kill) return true;
    return match.currentRound >= 3; // Force end after 3 rounds
  }

  private _determineMatchOutcome(match: ActiveMatch): MatchResult {
    const lastRound = match.roundResults[match.roundResults.length - 1];
    if (lastRound === RoundResult.Player1Kill) return MatchResult.Player1Win;
    if (lastRound === RoundResult.Player2Kill) return MatchResult.Player2Win;
    return MatchResult.Draw;
  }

  // ── DB Persistence ──

  private async _endMatch(match: ActiveMatch, outcome: MatchResult): Promise<{
    p1RatingDelta: number; p2RatingDelta: number; p1Coins: number; p2Coins: number;
  }> {
    const zero = { p1RatingDelta: 0, p2RatingDelta: 0, p1Coins: 0, p2Coins: 0 };

    // Guard against double-call (forfeit timer + normal end + surrender race)
    if (match.isEnded || this._endingMatches.has(match.matchId)) {
      this._logger.warn(`Match ${match.matchId} already ended or ending — skipping`);
      return zero;
    }
    this._endingMatches.add(match.matchId);
    match.isEnded = true;

    // Clear all timers immediately to prevent further race conditions
    if (match.revealTimer) { clearTimeout(match.revealTimer); match.revealTimer = null; }
    for (const timer of match.disconnectTimers.values()) clearTimeout(timer);
    match.disconnectTimers.clear();

    try {
      const dbOutcome = outcome === MatchResult.Player1Win
        ? 'PLAYER1_WIN'
        : outcome === MatchResult.Player2Win
          ? 'PLAYER2_WIN'
          : 'DRAW';

      const p1Outcome: 'win' | 'loss' | 'draw' =
        outcome === MatchResult.Player1Win ? 'win' : outcome === MatchResult.Player2Win ? 'loss' : 'draw';
      const p2Outcome: 'win' | 'loss' | 'draw' =
        outcome === MatchResult.Player2Win ? 'win' : outcome === MatchResult.Player1Win ? 'loss' : 'draw';

      // Atomic DB transaction: match status + player stats + coins
      const { p1RatingDelta, p2RatingDelta, p1Coins, p2Coins } = await this._prisma.$transaction(async (tx) => {
        await tx.match.update({
          where: { id: match.matchId },
          data: { status: 'COMPLETED', outcome: dbOutcome, endedAt: new Date() },
        });

        const { p1RatingDelta, p2RatingDelta } = await this._updatePlayerStatsInTx(tx, match, outcome);

        const coinsWin = p1Outcome === 'win' ? 50 : p1Outcome === 'loss' ? 10 : 25;
        const coinsLose = p2Outcome === 'win' ? 50 : p2Outcome === 'loss' ? 10 : 25;
        const p1 = await tx.player.update({
          where: { id: match.player1Id },
          data: { coins: { increment: coinsWin } },
          select: { coins: true },
        });
        const p2 = await tx.player.update({
          where: { id: match.player2Id },
          data: { coins: { increment: coinsLose } },
          select: { coins: true },
        });

        return { p1RatingDelta, p2RatingDelta, p1Coins: p1.coins, p2Coins: p2.coins };
      });

      // Hero mastery XP (non-critical, outside transaction)
      await this._players.awardHeroXP(match.player1Id, match.player1HeroId, p1Outcome)
        .catch((err) => this._logger.error(`Hero XP award failed for ${match.player1Id}: ${err}`));
      await this._players.awardHeroXP(match.player2Id, match.player2HeroId, p2Outcome)
        .catch((err) => this._logger.error(`Hero XP award failed for ${match.player2Id}: ${err}`));

      // On-chain settlement for staked matches
      if (match.stakeLevel > 0 && outcome !== MatchResult.Draw) {
        try {
          const winnerId = outcome === MatchResult.Player1Win ? match.player1Id : match.player2Id;
          const winner = await this._prisma.player.findUnique({
            where: { id: winnerId },
            select: { walletAddress: true },
          });
          if (winner?.walletAddress) {
            const totalStake = BigInt(match.stakeLevel) * 2n;
            const txSig = await this._blockchain.settleMatch(match.matchId, winner.walletAddress, totalStake);
            this._logger.log(`On-chain settlement: match=${match.matchId}, tx=${txSig}`);
          }
        } catch (err) {
          this._logger.error(`On-chain settlement failed for match ${match.matchId}: ${err}`);
        }
      }

      await this._removeCheckpoint(match.matchId);
      this._cleanupMatch(match);
      return { p1RatingDelta, p2RatingDelta, p1Coins, p2Coins };
    } catch (err) {
      this._logger.error(`_endMatch failed for ${match.matchId}: ${err}`);
      this._cleanupMatch(match);
      return zero;
    } finally {
      this._endingMatches.delete(match.matchId);
    }
  }

  private async _updatePlayerStatsInTx(
    tx: Prisma.TransactionClient,
    match: ActiveMatch,
    outcome: MatchResult,
  ): Promise<{ p1RatingDelta: number; p2RatingDelta: number }> {
    const K = 32;
    const p1 = await tx.player.findUnique({ where: { id: match.player1Id } });
    const p2 = await tx.player.findUnique({ where: { id: match.player2Id } });
    if (!p1 || !p2) return { p1RatingDelta: 0, p2RatingDelta: 0 };

    const expected1 = 1 / (1 + Math.pow(10, (p2.rating - p1.rating) / 400));
    const expected2 = 1 - expected1;

    let score1: number;
    let score2: number;
    if (outcome === MatchResult.Player1Win) { score1 = 1; score2 = 0; }
    else if (outcome === MatchResult.Player2Win) { score1 = 0; score2 = 1; }
    else { score1 = 0.5; score2 = 0.5; }

    const newRating1 = Math.round(p1.rating + K * (score1 - expected1));
    const newRating2 = Math.round(p2.rating + K * (score2 - expected2));

    await tx.player.update({
      where: { id: p1.id },
      data: {
        rating: newRating1,
        rankTier: MatchService._calculateRankTier(newRating1),
        wins: { increment: score1 === 1 ? 1 : 0 },
        losses: { increment: score1 === 0 ? 1 : 0 },
        draws: { increment: score1 === 0.5 ? 1 : 0 },
      },
    });
    await tx.player.update({
      where: { id: p2.id },
      data: {
        rating: newRating2,
        rankTier: MatchService._calculateRankTier(newRating2),
        wins: { increment: score2 === 1 ? 1 : 0 },
        losses: { increment: score2 === 0 ? 1 : 0 },
        draws: { increment: score2 === 0.5 ? 1 : 0 },
      },
    });

    return { p1RatingDelta: newRating1 - p1.rating, p2RatingDelta: newRating2 - p2.rating };
  }

  // ── Rank Tier ──

  private static _calculateRankTier(rating: number): number {
    if (rating >= 2000) return 6; // Grandmaster
    if (rating >= 1800) return 5; // Master
    if (rating >= 1600) return 4; // Diamond
    if (rating >= 1400) return 3; // Platinum
    if (rating >= 1200) return 2; // Gold
    if (rating >= 1000) return 1; // Silver
    return 0; // Bronze
  }

  // ── State Management ──

  /** Calculate danger zone tiles based on current round (shrinks from outer ring inward) */
  private _calculateShrinkZone(match: ActiveMatch): { x: number; y: number }[] {
    const round = match.currentRound;
    if (round < 3) return []; // No shrink for rounds 1-2

    const shrinkLevel = round - 2; // round 3 = level 1, round 4 = level 2, etc.
    const w = match.map.width;
    const h = match.map.height;
    const tiles: { x: number; y: number }[] = [];

    for (let layer = 0; layer < shrinkLevel && layer < Math.min(w, h) / 2; layer++) {
      for (let x = layer; x < w - layer; x++) {
        for (let y = layer; y < h - layer; y++) {
          // Only outer ring of this layer
          if (x === layer || x === w - 1 - layer || y === layer || y === h - 1 - layer) {
            tiles.push({ x, y });
          }
        }
      }
    }
    return tiles;
  }

  private _resetRoundState(match: ActiveMatch): void {
    match.p1Commit = null;
    match.p2Commit = null;
    match.p1Reveal = null;
    match.p2Reveal = null;
    match.phase = 'planning';
  }

  private _cleanupMatch(match: ActiveMatch): void {
    // Clear any pending timers
    for (const timer of match.disconnectTimers.values()) {
      clearTimeout(timer);
    }
    match.disconnectTimers.clear();
    if (match.revealTimer) { clearTimeout(match.revealTimer); match.revealTimer = null; }

    this._matches.delete(match.matchId);
    this._playerMatches.delete(match.player1Id);
    this._playerMatches.delete(match.player2Id);
  }

  // ── Payload Builders ──

  private _buildMatchPayload(match: ActiveMatch, isPlayer1: boolean): MatchFoundPayload {
    return {
      matchId: match.matchId,
      opponentName: isPlayer1 ? match.player2Name : match.player1Name,
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
      p1Shielded: step.p1Shielded,
      p2Shielded: step.p2Shielded,
    };
  }

  // ── Game Data ──

  private _roundOutcomeToDb(r: RoundResult): 'PLAYER1_KILL' | 'PLAYER2_KILL' | 'MUTUAL_CANCEL' | 'NO_KILL' {
    switch (r) {
      case RoundResult.Player1Kill: return 'PLAYER1_KILL';
      case RoundResult.Player2Kill: return 'PLAYER2_KILL';
      case RoundResult.MutualCancel: return 'MUTUAL_CANCEL';
      case RoundResult.NoKill: return 'NO_KILL';
    }
  }

  private _getRandomMap(): MapConfig {
    const maps = this._getAllMaps();
    return maps[Math.floor(Math.random() * maps.length)];
  }

  private _getAllMaps(): MapConfig[] {
    return [
      this._buildMap('arena_01', 10, 10, [vec2(1, 1), vec2(8, 8)], [Direction.Up, Direction.Left]),
      this._buildMap('arena_02', 8, 8, [vec2(1, 1), vec2(6, 6)], [Direction.Up, Direction.Left]),
      this._buildMap('arena_03', 10, 8, [vec2(1, 1), vec2(8, 6)], [Direction.Up, Direction.Left]),
    ];
  }

  private _buildMap(mapId: string, w: number, h: number, spawns: Vec2[], facings: Direction[]): MapConfig {
    const grid: TileType[][] = [];
    for (let x = 0; x < w; x++) {
      const col: TileType[] = [];
      for (let y = 0; y < h; y++) col.push(TileType.Empty);
      grid.push(col);
    }
    return { mapId, width: w, height: h, gridData: grid, pickups: [], spawnPoints: spawns, spawnFacings: facings };
  }

  private _getHeroConfig(heroId: string): HeroConfig {
    return getHeroConfig(heroId);
  }

  // ── Redis Checkpointing ──

  // TODO: MatchSnapshot does not capture GridSystem mutations (destroyed walls,
  // placed barriers/turrets, danger zones from shrink). After server restart,
  // the grid resets to original state. To fix properly, GridSystem needs a
  // serialize/restore interface. Low risk for now: crashes during active rounds
  // are rare, and players reconnect to fresh planning phase.
  private async _checkpointMatch(match: ActiveMatch): Promise<void> {
    const snapshot: MatchSnapshot = {
      matchId: match.matchId,
      roomId: match.roomId,
      player1Id: match.player1Id,
      player2Id: match.player2Id,
      player1Name: match.player1Name,
      player2Name: match.player2Name,
      player1HeroId: match.player1HeroId,
      player2HeroId: match.player2HeroId,
      map: match.map,
      currentRound: match.currentRound,
      roundResults: match.roundResults,
      phase: match.phase,
      stakeLevel: match.stakeLevel,
      heroStates: match.resolver.getHeroStates(),
    };
    const key = MatchService.REDIS_KEY_PREFIX + match.matchId;
    await this._redis.set(key, JSON.stringify(snapshot), MatchService.REDIS_TTL);
  }

  private async _removeCheckpoint(matchId: string): Promise<void> {
    await this._redis.del(MatchService.REDIS_KEY_PREFIX + matchId);
  }

  private async _restoreMatchesFromRedis(): Promise<void> {
    const keys = await this._redis.scanKeys(MatchService.REDIS_KEY_PREFIX + '*');
    if (keys.length === 0) return;

    this._logger.log(`Found ${keys.length} active match checkpoint(s) in Redis — restoring...`);
    let restored = 0;

    for (const key of keys) {
      try {
        const json = await this._redis.get(key);
        if (!json) continue;

        const snap: MatchSnapshot = JSON.parse(json);

        // Verify match is still IN_PROGRESS in DB
        const dbMatch = await this._prisma.match.findUnique({ where: { id: snap.matchId } });
        if (!dbMatch || dbMatch.status !== 'IN_PROGRESS') {
          await this._redis.del(key);
          continue;
        }

        const p1Config = this._getHeroConfig(snap.player1HeroId);
        const p2Config = this._getHeroConfig(snap.player2HeroId);
        const resolver = new ActionResolverService(
          snap.map, p1Config, p2Config,
          snap.map.spawnPoints[0], snap.map.spawnFacings[0],
          snap.map.spawnPoints[1], snap.map.spawnFacings[1],
        );
        resolver.restoreHeroStates(snap.heroStates.p1, snap.heroStates.p2);

        const active: ActiveMatch = {
          matchId: snap.matchId,
          roomId: snap.roomId,
          player1Id: snap.player1Id,
          player2Id: snap.player2Id,
          player1Name: snap.player1Name ?? 'Duelist',
          player2Name: snap.player2Name ?? 'Duelist',
          player1HeroId: snap.player1HeroId,
          player2HeroId: snap.player2HeroId,
          map: snap.map,
          currentRound: snap.currentRound,
          p1Commit: null,
          p2Commit: null,
          p1Reveal: null,
          p2Reveal: null,
          resolver,
          roundResults: snap.roundResults,
          disconnectTimers: new Map(),
          phase: 'planning', // Reset to planning — players need to reconnect
          isEnded: false,
          revealTimer: null,
          stakeLevel: snap.stakeLevel,
        };

        this._matches.set(snap.matchId, active);
        this._playerMatches.set(snap.player1Id, active);
        this._playerMatches.set(snap.player2Id, active);
        restored++;
      } catch (err) {
        this._logger.error(`Failed to restore match from ${key}: ${err}`);
        await this._redis.del(key);
      }
    }

    if (restored > 0) {
      this._logger.log(`Restored ${restored} active match(es) from Redis`);
    }
  }
}
