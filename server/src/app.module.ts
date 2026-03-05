import { Module } from '@nestjs/common';
import { PrismaModule } from './prisma/prisma.module';
import { AuthModule } from './auth/auth.module';
import { PlayerModule } from './player/player.module';
import { MatchModule } from './match/match.module';
import { ReplayModule } from './replay/replay.module';
import { SharedModule } from './shared/shared.module';

@Module({
  imports: [
    PrismaModule,
    SharedModule,
    AuthModule,
    PlayerModule,
    MatchModule,
    ReplayModule,
  ],
})
export class AppModule {}
