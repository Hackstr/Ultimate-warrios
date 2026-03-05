import { Injectable, Logger } from '@nestjs/common';

interface QueueEntry {
  playerId: string;
  heroId: string;
  rating: number;
  enqueuedAt: number;
}

export interface PairedResult {
  player1Id: string;
  player1HeroId: string;
  player2Id: string;
  player2HeroId: string;
}

/**
 * ELO-based matchmaking queue.
 * In-memory for MVP; production should use Redis sorted sets.
 * Rating window starts at ±200 and expands by 50 every 5 seconds.
 */
@Injectable()
export class MatchmakingService {
  private readonly _logger = new Logger(MatchmakingService.name);
  private readonly _queue: QueueEntry[] = [];

  private static readonly BASE_WINDOW = 200;
  private static readonly EXPAND_RATE = 50;
  private static readonly EXPAND_INTERVAL_MS = 5_000;

  /**
   * Adds a player to the queue and immediately attempts to pair.
   * Returns a PairedResult if a match is found, null otherwise.
   */
  async addToQueue(
    playerId: string,
    heroId: string,
    rating: number,
  ): Promise<PairedResult | null> {
    if (this._queue.some((e) => e.playerId === playerId)) {
      this._logger.warn(`Player ${playerId} already in queue`);
      return null;
    }

    const entry: QueueEntry = {
      playerId,
      heroId,
      rating,
      enqueuedAt: Date.now(),
    };

    const match = this._findMatch(entry);
    if (match) {
      this._removeEntry(match.playerId);
      this._logger.log(
        `Paired ${playerId} (${rating}) with ${match.playerId} (${match.rating})`,
      );
      return {
        player1Id: match.playerId,
        player1HeroId: match.heroId,
        player2Id: playerId,
        player2HeroId: heroId,
      };
    }

    this._queue.push(entry);
    this._logger.log(`Player ${playerId} added to queue (${this._queue.length} in queue)`);
    return null;
  }

  removeFromQueue(playerId: string): void {
    this._removeEntry(playerId);
  }

  get queueSize(): number {
    return this._queue.length;
  }

  private _findMatch(seeker: QueueEntry): QueueEntry | null {
    const now = Date.now();
    let bestMatch: QueueEntry | null = null;
    let bestDelta = Infinity;

    for (const candidate of this._queue) {
      if (candidate.playerId === seeker.playerId) continue;

      const elapsed = now - candidate.enqueuedAt;
      const expandedWindow =
        MatchmakingService.BASE_WINDOW +
        Math.floor(elapsed / MatchmakingService.EXPAND_INTERVAL_MS) *
          MatchmakingService.EXPAND_RATE;

      const delta = Math.abs(seeker.rating - candidate.rating);
      if (delta <= expandedWindow && delta < bestDelta) {
        bestMatch = candidate;
        bestDelta = delta;
      }
    }

    return bestMatch;
  }

  private _removeEntry(playerId: string): void {
    const idx = this._queue.findIndex((e) => e.playerId === playerId);
    if (idx !== -1) this._queue.splice(idx, 1);
  }
}
