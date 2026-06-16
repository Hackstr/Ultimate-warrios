import { Controller, Get, Post, Body, UseGuards, Request, NotFoundException } from '@nestjs/common';
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

  @UseGuards(JwtAuthGuard)
  @Post('unlock-hero')
  async unlockHero(
    @Request() req: { user: { playerId: string } },
    @Body() body: { heroId: string },
  ) {
    return this._players.unlockHero(req.user.playerId, body.heroId);
  }

  @Get('hero-prices')
  getHeroPrices() {
    return this._players.getHeroPrices();
  }

  @UseGuards(JwtAuthGuard)
  @Get('match-history')
  async getMatchHistory(@Request() req: { user: { playerId: string } }) {
    return this._players.getMatchHistory(req.user.playerId);
  }

  @UseGuards(JwtAuthGuard)
  @Get('daily-reward-status')
  async getDailyRewardStatus(@Request() req: { user: { playerId: string } }) {
    return this._players.getDailyRewardStatus(req.user.playerId);
  }

  @UseGuards(JwtAuthGuard)
  @Post('daily-reward')
  async claimDailyReward(@Request() req: { user: { playerId: string } }) {
    return this._players.claimDailyReward(req.user.playerId);
  }
}
