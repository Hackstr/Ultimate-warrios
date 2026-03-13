import { Injectable, Logger } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import * as crypto from 'crypto';
import { PrismaService } from '../prisma/prisma.service';

/**
 * Validates Telegram WebApp initData, issues JWTs, and manages player identity.
 */
@Injectable()
export class AuthService {
  private readonly _logger = new Logger(AuthService.name);

  constructor(
    private readonly _prisma: PrismaService,
    private readonly _jwt: JwtService,
  ) {}

  /**
   * Validates Telegram initData signature per TMA spec.
   * https://core.telegram.org/bots/webapps#validating-data-received-via-the-mini-app
   */
  validateTelegramInitData(initData: string, botToken: string): boolean {
    try {
      const params = new URLSearchParams(initData);
      const hash = params.get('hash');
      if (!hash) return false;

      params.delete('hash');
      const entries = Array.from(params.entries());
      entries.sort(([a], [b]) => a.localeCompare(b));
      const dataCheckString = entries.map(([k, v]) => `${k}=${v}`).join('\n');

      const secretKey = crypto
        .createHmac('sha256', 'WebAppData')
        .update(botToken)
        .digest();

      const computedHash = crypto
        .createHmac('sha256', secretKey)
        .update(dataCheckString)
        .digest('hex');

      return computedHash === hash;
    } catch (e) {
      this._logger.warn('initData validation failed', e);
      return false;
    }
  }

  /**
   * Extracts Telegram user data from validated initData.
   */
  extractUserFromInitData(initData: string): { id: string; username?: string; firstName?: string } | null {
    try {
      const params = new URLSearchParams(initData);
      const userJson = params.get('user');
      if (!userJson) return null;

      const user = JSON.parse(userJson);
      return {
        id: String(user.id),
        username: user.username,
        firstName: user.first_name,
      };
    } catch {
      return null;
    }
  }

  /**
   * Authenticates a Telegram user: validates initData, upserts Player, issues JWT.
   */
  async authenticateTelegram(initData: string, botToken: string): Promise<{ token: string; playerId: string } | null> {
    if (!this.validateTelegramInitData(initData, botToken)) {
      this._logger.warn('Invalid Telegram initData');
      return null;
    }

    const tgUser = this.extractUserFromInitData(initData);
    if (!tgUser) return null;

    const player = await this._prisma.player.upsert({
      where: { telegramId: tgUser.id },
      update: { username: tgUser.username ?? undefined },
      create: {
        telegramId: tgUser.id,
        username: tgUser.username,
        displayName: tgUser.firstName ?? 'Duelist',
      },
    });

    const token = this._jwt.sign({
      sub: player.id,
      telegramId: player.telegramId,
    });

    return { token, playerId: player.id };
  }

  /**
   * Verifies a JWT and returns the player ID, or null if invalid.
   */
  verifyToken(token: string): { sub: string; telegramId: string } | null {
    try {
      const payload = this._jwt.verify(token) as { sub?: string; telegramId?: string };
      if (!payload.sub || !payload.telegramId) return null;
      return { sub: payload.sub, telegramId: payload.telegramId };
    } catch {
      return null;
    }
  }

  /**
   * Development-only authentication. Creates/finds a player by username
   * and issues a JWT without Telegram validation.
   */
  async authenticateDev(username: string): Promise<{ token: string; playerId: string }> {
    const devTelegramId = `dev_${username}`;

    const player = await this._prisma.player.upsert({
      where: { telegramId: devTelegramId },
      update: {},
      create: {
        telegramId: devTelegramId,
        username,
        displayName: username,
      },
    });

    const token = this._jwt.sign({
      sub: player.id,
      telegramId: player.telegramId,
    });

    return { token, playerId: player.id };
  }
}
