import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { MatchGateway } from './match.gateway';
import { MatchService } from './match.service';
import { MatchmakingService } from './matchmaking.service';

@Module({
  imports: [AuthModule],
  providers: [MatchGateway, MatchService, MatchmakingService],
})
export class MatchModule {}
