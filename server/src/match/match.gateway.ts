import {
  WebSocketGateway,
  WebSocketServer,
  SubscribeMessage,
  MessageBody,
  ConnectedSocket,
  OnGatewayConnection,
  OnGatewayDisconnect,
  WsException,
} from '@nestjs/websockets';
import { UseGuards, UsePipes, ValidationPipe, Logger } from '@nestjs/common';
import { SkipThrottle } from '@nestjs/throttler';
import { Server, Socket } from 'socket.io';
import { WsAuthGuard } from '../auth/ws-auth.guard';
import { MatchService } from './match.service';
import { MatchmakingService } from './matchmaking.service';
import { PrismaService } from '../prisma/prisma.service';
import {
  FindMatchDto,
  RoundCommitDto,
  RoundRevealDto,
} from '../shared/models/dto';
import { VALID_HERO_IDS } from '../shared/config/hero-configs';

@SkipThrottle()
@WebSocketGateway({
  cors: {
    origin: process.env.CORS_ORIGIN
      ? process.env.CORS_ORIGIN.split(',').map((o) => o.trim())
      : '*',
    credentials: true,
  },
  transports: ['websocket', 'polling'],
})
export class MatchGateway implements OnGatewayConnection, OnGatewayDisconnect {
  private readonly _logger = new Logger(MatchGateway.name);

  @WebSocketServer()
  server!: Server;

  /** socket.id → playerId */
  private readonly _socketToPlayer = new Map<string, string>();
  /** playerId → socket.id */
  private readonly _playerToSocket = new Map<string, string>();

  constructor(
    private readonly _matchmaking: MatchmakingService,
    private readonly _matchService: MatchService,
    private readonly _prisma: PrismaService,
  ) {
    // Wire forfeit callback so MatchService can notify the opponent
    // when grace period expires after disconnect.
    this._matchService.setForfeitCallback((matchId, opponentId, winner) => {
      const oppSocket = this._getSocket(opponentId);
      if (oppSocket) {
        oppSocket.emit('match:end', { winner });
      }
    });
  }

  // ── Connection Lifecycle ──

  handleConnection(client: Socket) {
    this._logger.log(`Client connected: ${client.id}`);
  }

  handleDisconnect(client: Socket) {
    const playerId = this._socketToPlayer.get(client.id);
    this._logger.log(`Client disconnected: ${client.id} (player: ${playerId ?? 'unknown'})`);

    if (playerId) {
      this._socketToPlayer.delete(client.id);

      // Only process if THIS socket is the player's active socket.
      const activeSocketId = this._playerToSocket.get(playerId);
      if (activeSocketId === client.id) {
        this._matchmaking.removeFromQueue(playerId);
        this._playerToSocket.delete(playerId);

        // Start grace period instead of immediate cleanup
        const result = this._matchService.handleDisconnect(playerId);
        if (result) {
          // Notify opponent that the other player disconnected
          const oppSocket = this._getSocket(result.opponentId);
          if (oppSocket) {
            oppSocket.emit('match:opponent-disconnected', {
              matchId: result.matchId,
              gracePeriod: 60,
            });
          }
        }
      } else {
        this._logger.log(`Stale socket ${client.id} for player ${playerId}, active socket preserved`);
      }
    }
  }

  // ── Matchmaking ──

  @UseGuards(WsAuthGuard)
  @UsePipes(new ValidationPipe({ transform: true }))
  @SubscribeMessage('match:find')
  async handleFindMatch(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: FindMatchDto,
  ) {
    const playerId = client.data.playerId as string;
    this._registerSocket(client, playerId);

    if (!VALID_HERO_IDS.includes(data.heroId)) {
      throw new WsException(`Invalid heroId: "${data.heroId}"`);
    }

    const rating = await this._getPlayerRating(playerId);
    const paired = await this._matchmaking.addToQueue(playerId, data.heroId, rating);
    if (!paired) return;

    const match = await this._matchService.createMatch(
      paired.player1Id,
      paired.player1HeroId,
      paired.player2Id,
      paired.player2HeroId,
    );

    const p1Socket = this._getSocket(paired.player1Id);
    const p2Socket = this._getSocket(paired.player2Id);

    if (p1Socket) {
      p1Socket.emit('match:found', match.payloadForPlayer1);
      p1Socket.join(match.roomId);
    }
    if (p2Socket) {
      p2Socket.emit('match:found', match.payloadForPlayer2);
      p2Socket.join(match.roomId);
    }

    this.server.to(match.roomId).emit('round:start', {
      roundNumber: 1,
      timeLimit: 30,
    });
  }

  @UseGuards(WsAuthGuard)
  @SubscribeMessage('match:cancel')
  handleCancelMatch(@ConnectedSocket() client: Socket) {
    const playerId = client.data.playerId as string;
    this._matchmaking.removeFromQueue(playerId);
    return { event: 'match:cancel:ack', data: { success: true } };
  }

  // ── Round: Commit-Reveal ──

  @UseGuards(WsAuthGuard)
  @UsePipes(new ValidationPipe({ transform: true }))
  @SubscribeMessage('round:commit')
  async handleRoundCommit(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: RoundCommitDto,
  ) {
    const playerId = client.data.playerId as string;
    const result = await this._matchService.submitCommit(playerId, data.hash);

    if (!result.success) {
      throw new WsException(result.error ?? 'Commit failed');
    }

    if (result.bothCommitted) {
      const roomId = this._matchService.getRoomId(playerId);
      if (roomId) {
        this.server.to(roomId).emit('round:both-committed', {});
      }
    }
  }

  @UseGuards(WsAuthGuard)
  @UsePipes(new ValidationPipe({ transform: true }))
  @SubscribeMessage('round:reveal')
  async handleRoundReveal(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: RoundRevealDto,
  ) {
    const playerId = client.data.playerId as string;
    const result = await this._matchService.submitReveal(
      playerId,
      data.actions,
      data.nonce,
    );

    if (!result.success) {
      throw new WsException(result.error ?? 'Reveal failed');
    }

    if (result.roundResolved) {
      const roomId = this._matchService.getRoomId(playerId);
      if (!roomId) return;

      this.server.to(roomId).emit('round:results', {
        steps: result.steps,
      });

      this.server.to(roomId).emit('round:end', {
        result: result.roundOutcome,
      });

      if (result.matchEnded) {
        // Send per-player match end data (each sees their own ELO delta + coins)
        const p1Sock = result.player1Id ? this._getSocket(result.player1Id) : undefined;
        const p2Sock = result.player2Id ? this._getSocket(result.player2Id) : undefined;

        if (p1Sock) {
          p1Sock.emit('match:end', {
            winner: result.matchOutcome,
            ratingDelta: result.p1RatingDelta ?? 0,
            coinsEarned: result.p1Coins ?? 0,
          });
        }
        if (p2Sock) {
          p2Sock.emit('match:end', {
            winner: result.matchOutcome,
            ratingDelta: result.p2RatingDelta ?? 0,
            coinsEarned: result.p2Coins ?? 0,
          });
        }
      } else {
        const timeLimit = result.nextRound === 1 ? 30 : result.nextRound === 2 ? 25 : result.nextRound === 3 ? 20 : 15;
        this.server.to(roomId).emit('round:start', {
          roundNumber: result.nextRound,
          timeLimit,
          shrinkZone: result.shrinkZone,
        });
      }
    }
  }

  // ── Rejoin ──

  @UseGuards(WsAuthGuard)
  @SubscribeMessage('match:rejoin')
  handleRejoin(@ConnectedSocket() client: Socket) {
    const playerId = client.data.playerId as string;
    this._registerSocket(client, playerId);

    const state = this._matchService.rejoinMatch(playerId);
    if (!state) {
      client.emit('match:rejoin:ack', { success: false, error: 'No active match found' });
      return;
    }

    // Re-join the socket.io room
    client.join(state.roomId);

    // Notify opponent that player reconnected
    const opponentId = state.opponentId;
    const oppSocket = this._getSocket(opponentId);
    if (oppSocket) {
      oppSocket.emit('match:opponent-reconnected', { matchId: state.matchId });
    }

    client.emit('match:rejoin:ack', { success: true, state });
  }

  // ── Surrender ──

  @UseGuards(WsAuthGuard)
  @SubscribeMessage('match:surrender')
  async handleSurrender(@ConnectedSocket() client: Socket) {
    const playerId = client.data.playerId as string;
    try {
      // Get roomId BEFORE surrender (surrender cleans up match state)
      const roomId = this._matchService.getRoomId(playerId);
      const result = await this._matchService.surrender(playerId);
      if (result && roomId) {
        this.server.to(roomId).emit('match:end', { winner: result.winner });
      }
      client.emit('match:surrender:ack', { success: !!result });
    } catch (err) {
      this._logger.error(`Surrender failed for ${playerId}`, err);
      client.emit('match:surrender:ack', { success: false, error: 'Surrender failed' });
    }
  }

  // ── Helpers ──

  private _registerSocket(client: Socket, playerId: string) {
    this._socketToPlayer.set(client.id, playerId);
    this._playerToSocket.set(playerId, client.id);
  }

  private _getSocket(playerId: string): Socket | undefined {
    const socketId = this._playerToSocket.get(playerId);
    if (!socketId) return undefined;
    return this.server.sockets.sockets.get(socketId);
  }

  private async _getPlayerRating(playerId: string): Promise<number> {
    const player = await this._prisma.player.findUnique({
      where: { id: playerId },
      select: { rating: true },
    });
    return player?.rating ?? 1000;
  }
}
