import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { PlayerModule } from '../player/player.module';
import { BlockchainModule } from '../blockchain/blockchain.module';
import { MatchGateway } from './match.gateway';
import { MatchService } from './match.service';
import { MatchmakingService } from './matchmaking.service';

@Module({
  imports: [AuthModule, PlayerModule, BlockchainModule],
  providers: [MatchGateway, MatchService, MatchmakingService],
})
export class MatchModule {}
