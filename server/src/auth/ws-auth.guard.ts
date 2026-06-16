import { CanActivate, ExecutionContext, Injectable, Logger } from '@nestjs/common';
import { WsException } from '@nestjs/websockets';
import { Socket } from 'socket.io';
import { AuthService } from './auth.service';

/**
 * Guard for WebSocket connections.
 * Validates JWT from handshake auth only (query params disabled for security).
 * Attaches playerId to socket.data for downstream handlers.
 */
@Injectable()
export class WsAuthGuard implements CanActivate {
  private readonly _logger = new Logger(WsAuthGuard.name);

  constructor(private readonly _auth: AuthService) {}

  canActivate(context: ExecutionContext): boolean {
    const client = context.switchToWs().getClient<Socket>();
    const token = client.handshake.auth?.token;

    if (!token || typeof token !== 'string') {
      this._logger.warn(`WS auth failed: no token (${client.id})`);
      throw new WsException('Missing authentication token');
    }

    const payload = this._auth.verifyToken(token);
    if (!payload) {
      this._logger.warn(`WS auth failed: invalid token (${client.id})`);
      throw new WsException('Invalid authentication token');
    }

    client.data.playerId = payload.sub;
    client.data.telegramId = payload.telegramId;
    return true;
  }
}
