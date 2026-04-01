import { Controller, Get, Post, Body, UseGuards, Request } from '@nestjs/common';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { BlockchainService } from './blockchain.service';
import { PrismaService } from '../prisma/prisma.service';

@Controller('blockchain')
export class BlockchainController {
  constructor(
    private readonly _blockchain: BlockchainService,
    private readonly _prisma: PrismaService,
  ) {}

  /** Get blockchain provider info. */
  @Get('info')
  getInfo() {
    return {
      provider: this._blockchain.providerName,
      network: this._blockchain.provider.network,
    };
  }

  /** Connect wallet — save address to player profile. */
  @UseGuards(JwtAuthGuard)
  @Post('connect-wallet')
  async connectWallet(
    @Request() req: { user: { playerId: string } },
    @Body() body: { walletAddress: string },
  ) {
    if (!this._blockchain.provider.isValidAddress(body.walletAddress)) {
      return { success: false, error: 'Invalid wallet address' };
    }

    await this._prisma.player.update({
      where: { id: req.user.playerId },
      data: {
        walletAddress: body.walletAddress,
        walletChain: this._blockchain.providerName,
      },
    });

    return { success: true, chain: this._blockchain.providerName };
  }

  /** Get balance for connected wallet. */
  @UseGuards(JwtAuthGuard)
  @Post('balance')
  async getBalance(@Body() body: { address: string }) {
    const balance = await this._blockchain.getBalance(body.address);
    return { balance: balance.toString() };
  }

  /** Create a stake transaction for client to sign. */
  @UseGuards(JwtAuthGuard)
  @Post('create-stake')
  async createStake(
    @Request() req: { user: { playerId: string } },
    @Body() body: { matchId: string; walletAddress: string; amount: string },
  ) {
    const serializedTx = await this._blockchain.createStakeTransaction(
      body.matchId,
      body.walletAddress,
      BigInt(body.amount),
    );
    return { transaction: serializedTx };
  }

  /** Verify a stake transaction was confirmed. */
  @UseGuards(JwtAuthGuard)
  @Post('verify-stake')
  async verifyStake(
    @Body() body: { txSignature: string; expectedAmount: string },
  ) {
    const verified = await this._blockchain.verifyStake(
      body.txSignature,
      BigInt(body.expectedAmount),
    );
    return { verified };
  }

  /** Get explorer URL for a transaction. */
  @Post('explorer-url')
  getExplorerUrl(@Body() body: { txSignature: string }) {
    return { url: this._blockchain.getExplorerUrl(body.txSignature) };
  }
}
