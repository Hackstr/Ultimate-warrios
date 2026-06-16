import { Controller, Post, Body, UnauthorizedException, BadRequestException } from '@nestjs/common';
import { Throttle } from '@nestjs/throttler';
import { AuthService } from './auth.service';

class TelegramAuthDto {
  initData!: string;
}

class DevAuthDto {
  username!: string;
}

@Controller('auth')
export class AuthController {
  constructor(private readonly _auth: AuthService) {}

  @Post('telegram')
  @Throttle({ default: { ttl: 60000, limit: 10 } })
  async authenticateTelegram(@Body() dto: TelegramAuthDto) {
    const botToken = process.env.TELEGRAM_BOT_TOKEN;
    if (!botToken) throw new UnauthorizedException('Bot token not configured');

    const result = await this._auth.authenticateTelegram(dto.initData, botToken);
    if (!result) throw new UnauthorizedException('Invalid Telegram credentials');

    return result;
  }

  @Post('dev')
  @Throttle({ default: { ttl: 60000, limit: 5 } })
  async authenticateDev(@Body() dto: DevAuthDto) {
    if (process.env.NODE_ENV === 'production') {
      throw new UnauthorizedException('Dev auth is disabled in production');
    }

    if (!dto.username || typeof dto.username !== 'string') {
      throw new BadRequestException('username is required');
    }

    return this._auth.authenticateDev(dto.username);
  }
}
