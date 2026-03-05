import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { PlayerService } from './player.service';
import { PlayerController } from './player.controller';

@Module({
  imports: [AuthModule],
  controllers: [PlayerController],
  providers: [PlayerService],
  exports: [PlayerService],
})
export class PlayerModule {}
