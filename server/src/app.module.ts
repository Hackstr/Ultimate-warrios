import { Module } from '@nestjs/common';
import { APP_GUARD } from '@nestjs/core';
import { ThrottlerModule, ThrottlerGuard } from '@nestjs/throttler';
import { PrismaModule } from './prisma/prisma.module';
import { AuthModule } from './auth/auth.module';
import { PlayerModule } from './player/player.module';
import { MatchModule } from './match/match.module';
import { SharedModule } from './shared/shared.module';
import { BlockchainModule } from './blockchain/blockchain.module';

@Module({
  imports: [
    ThrottlerModule.forRoot([{
      ttl: 60000,  // 1 minute window
      limit: 30,   // 30 requests per minute (global default)
    }]),
    PrismaModule,
    SharedModule,
    AuthModule,
    PlayerModule,
    MatchModule,
    BlockchainModule,
  ],
  providers: [
    {
      provide: APP_GUARD,
      useClass: ThrottlerGuard,
    },
  ],
})
export class AppModule {}
