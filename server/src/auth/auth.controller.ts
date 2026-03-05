import { Controller, Post, Body, UnauthorizedException } from '@nestjs/common';
import { AuthService } from './auth.service';

class TelegramAuthDto {
  initData!: string;
}

@Controller('auth')
export class AuthController {
  constructor(private readonly _auth: AuthService) {}

  @Post('telegram')
  async authenticateTelegram(@Body() dto: TelegramAuthDto) {
    const botToken = process.env.TELEGRAM_BOT_TOKEN;
    if (!botToken) throw new UnauthorizedException('Bot token not configured');

    const result = await this._auth.authenticateTelegram(dto.initData, botToken);
    if (!result) throw new UnauthorizedException('Invalid Telegram credentials');

    return result;
  }
}
