import { NestFactory } from '@nestjs/core';
import { IoAdapter } from '@nestjs/platform-socket.io';
import { Logger } from '@nestjs/common';
import { AppModule } from './app.module';

async function bootstrap() {
  const logger = new Logger('Bootstrap');
  const app = await NestFactory.create(AppModule);

  const corsOrigin = process.env.CORS_ORIGIN;
  app.enableCors({
    origin: corsOrigin ? corsOrigin.split(',').map((o) => o.trim()) : '*',
    credentials: true,
  });

  app.useWebSocketAdapter(new IoAdapter(app));

  app.enableShutdownHooks();

  const port = process.env.PORT ?? 3000;
  await app.listen(port);
  logger.log(`Server listening on port ${port}`);

  if (!corsOrigin) {
    logger.warn('CORS_ORIGIN not set — accepting requests from any origin');
  }
}

bootstrap().catch((err) => {
  Logger.error(`Failed to start server: ${err.message}`, err.stack, 'Bootstrap');
  process.exit(1);
});
