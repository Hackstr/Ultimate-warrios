import { Injectable, BadRequestException } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

/** Coins awarded per match outcome */
const COINS_WIN = 50;
const COINS_LOSS = 10;
const COINS_DRAW = 25;

/** Hero unlock prices (coins) */
const HERO_PRICES: Record<string, number> = {
  archer: 0,    // starter — free
  tank: 0,      // starter — free
  scout: 0,     // starter — free
  shadow: 200,
  mage: 200,
  demo: 300,
  guardian: 300,
  ghost: 400,
  engineer: 400,
  berserker: 500,
  hawk: 500,
  mirage: 600,
};

@Injectable()
export class PlayerService {
  constructor(private readonly _prisma: PrismaService) {}

  findById(id: string) {
    return this._prisma.player.findUnique({ where: { id } });
  }

  findByTelegramId(telegramId: string) {
    return this._prisma.player.findUnique({ where: { telegramId } });
  }

  static calculateRankTier(rating: number): number {
    if (rating >= 2000) return 6; // Grandmaster
    if (rating >= 1800) return 5; // Master
    if (rating >= 1600) return 4; // Diamond
    if (rating >= 1400) return 3; // Platinum
    if (rating >= 1200) return 2; // Gold
    if (rating >= 1000) return 1; // Silver
    return 0; // Bronze
  }

  async getProfile(id: string) {
    const player = await this._prisma.player.findUnique({
      where: { id },
      include: { masteries: true },
    });
    if (!player) return null;

    // Compute favorite hero from match history
    const favoriteHero = await this._getFavoriteHero(id);

    return {
      ...player,
      rankTier: PlayerService.calculateRankTier(player.rating),
      favoriteHero,
      heroPrices: HERO_PRICES,
    };
  }

  async getLeaderboard(limit = 20) {
    return this._prisma.player.findMany({
      orderBy: { rating: 'desc' },
      take: limit,
      select: {
        id: true,
        displayName: true,
        rating: true,
        wins: true,
        losses: true,
        rankTier: true,
      },
    });
  }

  /** Award hero mastery XP after a match. */
  async awardHeroXP(playerId: string, heroId: string, outcome: 'win' | 'loss' | 'draw'): Promise<void> {
    const xpAmount = outcome === 'win' ? 30 : outcome === 'loss' ? 10 : 15;
    try {
      // Single upsert that returns updated XP, then conditionally update level
      const mastery = await this._prisma.heroMastery.upsert({
        where: { playerId_heroId: { playerId, heroId } },
        create: { playerId, heroId, xp: xpAmount, level: 1 },
        update: { xp: { increment: xpAmount } },
      });
      const newLevel = mastery.xp >= 500 ? 5 : mastery.xp >= 250 ? 4 : mastery.xp >= 100 ? 3 : mastery.xp >= 50 ? 2 : 1;
      if (newLevel !== mastery.level) {
        await this._prisma.heroMastery.update({
          where: { id: mastery.id },
          data: { level: newLevel },
        });
      }
    } catch {
      // Non-critical — log and continue
    }
  }

  /** Award coins after a match ends. Called by MatchService. */
  async awardCoins(playerId: string, outcome: 'win' | 'loss' | 'draw'): Promise<number> {
    const amount = outcome === 'win' ? COINS_WIN : outcome === 'loss' ? COINS_LOSS : COINS_DRAW;
    const player = await this._prisma.player.update({
      where: { id: playerId },
      data: { coins: { increment: amount } },
      select: { coins: true },
    });
    return player.coins;
  }

  /** Unlock a hero by spending coins. */
  async unlockHero(playerId: string, heroId: string): Promise<{ success: boolean; coins: number; unlockedHeroes: string[] }> {
    const price = HERO_PRICES[heroId];
    if (price === undefined) {
      throw new BadRequestException(`Unknown hero: ${heroId}`);
    }
    if (price === 0) {
      throw new BadRequestException(`Hero ${heroId} is free and already unlocked`);
    }

    const player = await this._prisma.player.findUnique({
      where: { id: playerId },
      select: { coins: true, unlockedHeroes: true },
    });
    if (!player) throw new BadRequestException('Player not found');

    if (player.unlockedHeroes.includes(heroId)) {
      throw new BadRequestException(`Hero ${heroId} already unlocked`);
    }
    if (player.coins < price) {
      throw new BadRequestException(`Not enough coins (have ${player.coins}, need ${price})`);
    }

    const updated = await this._prisma.player.update({
      where: { id: playerId },
      data: {
        coins: { decrement: price },
        unlockedHeroes: { push: heroId },
      },
      select: { coins: true, unlockedHeroes: true },
    });

    return { success: true, coins: updated.coins, unlockedHeroes: updated.unlockedHeroes };
  }

  /** Get hero prices list. */
  getHeroPrices() {
    return HERO_PRICES;
  }

  // ── Daily Rewards ──

  private static readonly DAILY_REWARDS = [25, 50, 75, 100, 150, 200, 300];

  async getDailyRewardStatus(playerId: string): Promise<{
    streak: number; canClaim: boolean; nextReward: number;
  }> {
    const player = await this._prisma.player.findUnique({
      where: { id: playerId },
      select: { lastDailyReward: true, loginStreak: true },
    });
    if (!player) throw new BadRequestException('Player not found');

    const today = this._todayUtc();
    const lastClaim = player.lastDailyReward ? this._dateToUtcDay(player.lastDailyReward) : null;
    const claimedToday = lastClaim !== null && lastClaim.getTime() === today.getTime();
    const isConsecutive = lastClaim !== null && lastClaim.getTime() === this._yesterdayUtc().getTime();
    const nextStreak = claimedToday
      ? player.loginStreak
      : isConsecutive
        ? (player.loginStreak % 7) + 1
        : 1;

    return {
      streak: player.loginStreak,
      canClaim: !claimedToday,
      nextReward: PlayerService.DAILY_REWARDS[nextStreak - 1],
    };
  }

  async claimDailyReward(playerId: string): Promise<{
    coins: number; streak: number; reward: number; unlockedHero: string | null;
  }> {
    const player = await this._prisma.player.findUnique({
      where: { id: playerId },
      select: { lastDailyReward: true, loginStreak: true, coins: true, unlockedHeroes: true },
    });
    if (!player) throw new BadRequestException('Player not found');

    const today = this._todayUtc();
    const lastClaim = player.lastDailyReward ? this._dateToUtcDay(player.lastDailyReward) : null;

    if (lastClaim !== null && lastClaim.getTime() === today.getTime()) {
      return { coins: player.coins, streak: player.loginStreak, reward: 0, unlockedHero: null };
    }

    const isConsecutive = lastClaim !== null && lastClaim.getTime() === this._yesterdayUtc().getTime();
    const newStreak = isConsecutive ? (player.loginStreak % 7) + 1 : 1;
    const rewardAmount = PlayerService.DAILY_REWARDS[newStreak - 1];

    // Day 7 bonus: unlock a random locked hero
    let unlockedHero: string | null = null;
    if (newStreak === 7) {
      const allPaid = Object.keys(HERO_PRICES).filter((h) => HERO_PRICES[h] > 0);
      const locked = allPaid.filter((h) => !player.unlockedHeroes.includes(h));
      if (locked.length > 0) {
        unlockedHero = locked[Math.floor(Math.random() * locked.length)];
      }
    }

    const updateData: Record<string, unknown> = {
      coins: { increment: rewardAmount },
      lastDailyReward: new Date(),
      loginStreak: newStreak,
    };
    if (unlockedHero) {
      updateData.unlockedHeroes = { push: unlockedHero };
    }

    const updated = await this._prisma.player.update({
      where: { id: playerId },
      data: updateData,
      select: { coins: true, loginStreak: true },
    });

    return {
      coins: updated.coins,
      streak: updated.loginStreak,
      reward: rewardAmount,
      unlockedHero,
    };
  }

  private _todayUtc(): Date {
    const now = new Date();
    return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  }

  private _yesterdayUtc(): Date {
    const d = this._todayUtc();
    d.setUTCDate(d.getUTCDate() - 1);
    return d;
  }

  private _dateToUtcDay(date: Date): Date {
    return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
  }

  /** Get recent match history for a player. */
  async getMatchHistory(playerId: string, limit = 20) {
    const matches = await this._prisma.match.findMany({
      where: {
        OR: [{ player1Id: playerId }, { player2Id: playerId }],
        status: 'COMPLETED',
      },
      orderBy: { endedAt: 'desc' },
      take: limit,
      select: {
        id: true,
        player1Hero: true,
        player2Hero: true,
        outcome: true,
        mapId: true,
        startedAt: true,
        endedAt: true,
        player1Id: true,
        player2Id: true,
        player1: { select: { displayName: true } },
        player2: { select: { displayName: true } },
      },
    });

    return matches.map((m) => ({
      matchId: m.id,
      isPlayer1: m.player1Id === playerId,
      yourHero: m.player1Id === playerId ? m.player1Hero : m.player2Hero,
      opponentHero: m.player1Id === playerId ? m.player2Hero : m.player1Hero,
      opponentName: m.player1Id === playerId ? m.player2.displayName : m.player1.displayName,
      result: m.outcome === 'DRAW' ? 'draw' :
        (m.player1Id === playerId && m.outcome === 'PLAYER1_WIN') ||
        (m.player2Id === playerId && m.outcome === 'PLAYER2_WIN') ? 'win' : 'loss',
      mapId: m.mapId,
      date: m.endedAt?.toISOString() ?? m.startedAt.toISOString(),
    }));
  }

  private async _getFavoriteHero(playerId: string): Promise<string | null> {
    const result = await this._prisma.$queryRaw<{ hero: string; cnt: bigint }[]>`
      SELECT hero, COUNT(*) as cnt FROM (
        SELECT "player1Hero" AS hero FROM "Match"
          WHERE "player1Id" = ${playerId} AND status = 'COMPLETED'
        UNION ALL
        SELECT "player2Hero" AS hero FROM "Match"
          WHERE "player2Id" = ${playerId} AND status = 'COMPLETED'
      ) AS heroes
      GROUP BY hero
      ORDER BY cnt DESC
      LIMIT 1
    `;
    return result.length > 0 ? result[0].hero : null;
  }
}
