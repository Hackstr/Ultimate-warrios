import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

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
    return this._prisma.player.findUnique({
      where: { id },
      include: { masteries: true },
    });
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
}
