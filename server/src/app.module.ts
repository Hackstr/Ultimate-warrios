import { Module } from '@nestjs/common';
import { PrismaModule } from './prisma/prisma.module';
import { AuthModule } from './auth/auth.module';
import { PlayerModule } from './player/player.module';
import { MatchModule } from './match/match.module';
import { SharedModule } from './shared/shared.module';
import { BlockchainModule } from './blockchain/blockchain.module';

@Module({
  imports: [
    PrismaModule,
    SharedModule,
    AuthModule,
    PlayerModule,
    MatchModule,
    BlockchainModule,
  ],
})
export class AppModule {}
