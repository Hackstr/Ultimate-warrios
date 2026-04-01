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
      await this._prisma.heroMastery.upsert({
        where: { playerId_heroId: { playerId, heroId } },
        create: { playerId, heroId, xp: xpAmount, level: 1 },
        update: {
          xp: { increment: xpAmount },
          level: { set: undefined }, // Will calculate below
        },
      });
      // Update level based on XP thresholds: 0-99 = L1, 100-249 = L2, 250-499 = L3, 500+ = L4
      const mastery = await this._prisma.heroMastery.findUnique({
        where: { playerId_heroId: { playerId, heroId } },
      });
      if (mastery) {
        const newLevel = mastery.xp >= 500 ? 5 : mastery.xp >= 250 ? 4 : mastery.xp >= 100 ? 3 : mastery.xp >= 50 ? 2 : 1;
        if (newLevel !== mastery.level) {
          await this._prisma.heroMastery.update({
            where: { id: mastery.id },
            data: { level: newLevel },
          });
        }
      }
    } catch (err) {
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
    // Find the hero this player used most across matches
    const asP1 = await this._prisma.match.groupBy({
      by: ['player1Hero'],
      where: { player1Id: playerId, status: 'COMPLETED' },
      _count: true,
      orderBy: { _count: { player1Hero: 'desc' } },
      take: 1,
    });
    const asP2 = await this._prisma.match.groupBy({
      by: ['player2Hero'],
      where: { player2Id: playerId, status: 'COMPLETED' },
      _count: true,
      orderBy: { _count: { player2Hero: 'desc' } },
      take: 1,
    });

    const counts: Record<string, number> = {};
    if (asP1.length > 0) counts[asP1[0].player1Hero] = (counts[asP1[0].player1Hero] ?? 0) + asP1[0]._count;
    if (asP2.length > 0) counts[asP2[0].player2Hero] = (counts[asP2[0].player2Hero] ?? 0) + asP2[0]._count;

    let best: string | null = null;
    let bestCount = 0;
    for (const [hero, count] of Object.entries(counts)) {
      if (count > bestCount) { best = hero; bestCount = count; }
    }
    return best;
  }
}
