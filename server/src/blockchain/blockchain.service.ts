import { Injectable, Logger } from '@nestjs/common';
import { SolanaProvider } from './solana.provider';

/**
 * Blockchain-agnostic service. Routes calls to the active provider
 * (Solana for hackathon/standalone, TON for TMA — selected via env).
 */

export interface IBlockchainProvider {
  readonly name: string;
  readonly network: string;

  /** Verify a wallet address is valid. */
  isValidAddress(address: string): boolean;

  /** Get native balance (lamports for SOL, nanotons for TON). */
  getBalance(address: string): Promise<bigint>;

  /** Create escrow for a staked match. Returns serialized transaction for client to sign. */
  createStakeTransaction(matchId: string, playerAddress: string, amount: bigint): Promise<string>;

  /** Verify a stake transaction was confirmed on-chain. */
  verifyStakeTransaction(txSignature: string, expectedAmount: bigint): Promise<boolean>;

  /** Settle match: release escrow to winner. Server-signed. */
  settleMatch(matchId: string, winnerAddress: string, totalAmount: bigint): Promise<string>;

  /** Get explorer URL for a transaction. */
  getExplorerUrl(txSignature: string): string;
}

@Injectable()
export class BlockchainService {
  private readonly _logger = new Logger(BlockchainService.name);
  private readonly _provider: IBlockchainProvider;

  constructor() {
    const providerName = process.env.BLOCKCHAIN_PROVIDER || 'solana';

    switch (providerName) {
      case 'solana':
        this._provider = new SolanaProvider(
          process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com',
          process.env.SOLANA_PROGRAM_ID || '',
          process.env.SOLANA_AUTHORITY_KEY || '',
        );
        break;
      // case 'ton':
      //   this._provider = new TonProvider(...);
      //   break;
      default:
        this._provider = new SolanaProvider(
          'https://api.devnet.solana.com', '', '',
        );
    }

    this._logger.log(`Blockchain provider: ${this._provider.name} (${this._provider.network})`);
  }

  get provider(): IBlockchainProvider {
    return this._provider;
  }

  get providerName(): string {
    return this._provider.name;
  }

  async getBalance(address: string): Promise<bigint> {
    return this._provider.getBalance(address);
  }

  async createStakeTransaction(matchId: string, playerAddress: string, amount: bigint): Promise<string> {
    return this._provider.createStakeTransaction(matchId, playerAddress, amount);
  }

  async verifyStake(txSignature: string, expectedAmount: bigint): Promise<boolean> {
    return this._provider.verifyStakeTransaction(txSignature, expectedAmount);
  }

  async settleMatch(matchId: string, winnerAddress: string, totalAmount: bigint): Promise<string> {
    return this._provider.settleMatch(matchId, winnerAddress, totalAmount);
  }

  getExplorerUrl(txSignature: string): string {
    return this._provider.getExplorerUrl(txSignature);
  }
}
