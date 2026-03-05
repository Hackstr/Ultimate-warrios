import { Controller, Get, UseGuards, Request, NotFoundException } from '@nestjs/common';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { PlayerService } from './player.service';

@Controller('player')
export class PlayerController {
  constructor(private readonly _players: PlayerService) {}

  @UseGuards(JwtAuthGuard)
  @Get('me')
  async getMyProfile(@Request() req: { user: { playerId: string } }) {
    const profile = await this._players.getProfile(req.user.playerId);
    if (!profile) throw new NotFoundException('Player not found');
    return profile;
  }

  @Get('leaderboard')
  async getLeaderboard() {
    return this._players.getLeaderboard();
  }
}
