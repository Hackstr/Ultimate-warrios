import { Injectable, NotFoundException } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

@Injectable()
export class ReplayService {
  constructor(private readonly _prisma: PrismaService) {}

  async getByMatchId(matchId: string) {
    const replay = await this._prisma.replay.findUnique({
      where: { matchId },
    });
    if (!replay) throw new NotFoundException(`Replay for match ${matchId} not found`);
    return JSON.parse(replay.dataJson);
  }

  async getById(replayId: string) {
    const replay = await this._prisma.replay.findUnique({
      where: { id: replayId },
    });
    if (!replay) throw new NotFoundException(`Replay ${replayId} not found`);
    return JSON.parse(replay.dataJson);
  }
}
