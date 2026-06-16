import { Injectable, OnModuleInit, OnModuleDestroy, Logger } from '@nestjs/common';
import Redis from 'ioredis';

@Injectable()
export class RedisService implements OnModuleInit, OnModuleDestroy {
  private readonly _logger = new Logger(RedisService.name);
  public client!: Redis;

  get isConnected(): boolean {
    return this.client?.status === 'ready';
  }

  onModuleInit() {
    const password = process.env.REDIS_PASSWORD;
    this.client = new Redis({
      host: process.env.REDIS_HOST ?? 'localhost',
      port: Number(process.env.REDIS_PORT ?? 6379),
      password: password === '' || !password ? undefined : password,
      lazyConnect: true,
      maxRetriesPerRequest: 3,
      retryStrategy: (times) => Math.min(times * 500, 5000),
    });

    this.client.on('error', (err) => {
      this._logger.error(`Redis error: ${err.message}`);
    });

    this.client
      .connect()
      .then(() => this._logger.log('Redis connected'))
      .catch((err) => this._logger.warn(`Redis connection failed (non-fatal): ${err.message}`));
  }

  async onModuleDestroy() {
    await this.client?.quit().catch(() => {});
  }

  async set(key: string, value: string, ttlSeconds?: number): Promise<boolean> {
    if (!this.isConnected) return false;
    try {
      if (ttlSeconds) await this.client.set(key, value, 'EX', ttlSeconds);
      else await this.client.set(key, value);
      return true;
    } catch (err) {
      this._logger.error(`Redis SET failed for key "${key}": ${err}`);
      return false;
    }
  }

  async get(key: string): Promise<string | null> {
    if (!this.isConnected) return null;
    try {
      return await this.client.get(key);
    } catch (err) {
      this._logger.error(`Redis GET failed for key "${key}": ${err}`);
      return null;
    }
  }

  async del(key: string): Promise<boolean> {
    if (!this.isConnected) return false;
    try {
      await this.client.del(key);
      return true;
    } catch (err) {
      this._logger.error(`Redis DEL failed for key "${key}": ${err}`);
      return false;
    }
  }

  async exists(key: string): Promise<boolean> {
    if (!this.isConnected) return false;
    try {
      return (await this.client.exists(key)) === 1;
    } catch (err) {
      this._logger.error(`Redis EXISTS failed for key "${key}": ${err}`);
      return false;
    }
  }

  /** Non-blocking key scan (uses SCAN instead of KEYS to avoid blocking Redis). */
  async scanKeys(pattern: string): Promise<string[]> {
    if (!this.isConnected) return [];
    try {
      const keys: string[] = [];
      let cursor = '0';
      do {
        const [nextCursor, batch] = await this.client.scan(cursor, 'MATCH', pattern, 'COUNT', 100);
        keys.push(...batch);
        cursor = nextCursor;
      } while (cursor !== '0');
      return keys;
    } catch (err) {
      this._logger.error(`Redis SCAN failed for pattern "${pattern}": ${err}`);
      return [];
    }
  }
}
