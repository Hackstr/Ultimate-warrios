import { Logger } from '@nestjs/common';
import {
  Connection,
  PublicKey,
  Keypair,
  Transaction,
  SystemProgram,
  LAMPORTS_PER_SOL,
  sendAndConfirmTransaction,
} from '@solana/web3.js';
import * as bs58 from 'bs58';
import { IBlockchainProvider } from './blockchain.service';

/**
 * Solana blockchain provider.
 * Handles wallet validation, balance queries, escrow creation, and settlement.
 *
 * Two modes:
 * - Without program ID: uses simple SOL transfers (for testing/hackathon MVP)
 * - With program ID: uses Anchor program for full escrow (production)
 */
export class SolanaProvider implements IBlockchainProvider {
  private readonly _logger = new Logger(SolanaProvider.name);
  private readonly _connection: Connection;
  private readonly _programId: string;
  private readonly _authority: Keypair | null;
  private readonly _treasuryAddress: string;

  constructor(rpcUrl: string, programId: string, authorityKey: string) {
    this._connection = new Connection(rpcUrl, 'confirmed');
    this._programId = programId;

    // Load authority keypair from base58 private key
    if (authorityKey) {
      try {
        const decoded = bs58.decode(authorityKey);
        this._authority = Keypair.fromSecretKey(decoded);
        this._treasuryAddress = this._authority.publicKey.toString();
        this._logger.log(`Authority wallet: ${this._treasuryAddress}`);
      } catch {
        this._logger.warn('Invalid SOLANA_AUTHORITY_KEY — settlement will be disabled');
        this._authority = null;
        this._treasuryAddress = '';
      }
    } else {
      this._authority = null;
      this._treasuryAddress = '';
      this._logger.warn('No SOLANA_AUTHORITY_KEY — running in read-only mode');
    }

    this._logger.log(`Solana RPC: ${rpcUrl}`);
    if (programId) this._logger.log(`Program ID: ${programId}`);
  }

  get name(): string { return 'solana'; }
  get network(): string {
    const url = this._connection.rpcEndpoint;
    if (url.includes('devnet')) return 'devnet';
    if (url.includes('testnet')) return 'testnet';
    return 'mainnet-beta';
  }

  isValidAddress(address: string): boolean {
    try {
      new PublicKey(address);
      return true;
    } catch {
      return false;
    }
  }

  async getBalance(address: string): Promise<bigint> {
    try {
      const pubkey = new PublicKey(address);
      const balance = await this._connection.getBalance(pubkey);
      return BigInt(balance);
    } catch (err) {
      this._logger.error(`getBalance failed: ${err}`);
      return 0n;
    }
  }

  /**
   * Create a stake transaction: transfers SOL from player to treasury.
   * Returns base64-encoded serialized transaction for client to sign.
   *
   * MVP mode: Simple SOL transfer to treasury.
   * Production: Would create Anchor escrow instruction.
   */
  async createStakeTransaction(
    matchId: string,
    playerAddress: string,
    amount: bigint,
  ): Promise<string> {
    if (!this._treasuryAddress) {
      this._logger.error('Cannot create stake: no treasury address');
      return '';
    }

    try {
      const playerPubkey = new PublicKey(playerAddress);
      const treasuryPubkey = new PublicKey(this._treasuryAddress);

      const transaction = new Transaction().add(
        SystemProgram.transfer({
          fromPubkey: playerPubkey,
          toPubkey: treasuryPubkey,
          lamports: Number(amount),
        }),
      );

      // Set recent blockhash
      const { blockhash, lastValidBlockHeight } = await this._connection.getLatestBlockhash();
      transaction.recentBlockhash = blockhash;
      transaction.lastValidBlockHeight = lastValidBlockHeight;
      transaction.feePayer = playerPubkey;

      // Serialize for client to sign
      const serialized = transaction.serialize({
        requireAllSignatures: false,
        verifySignatures: false,
      });

      this._logger.log(`Created stake TX: match=${matchId}, player=${playerAddress}, amount=${amount} lamports`);
      return Buffer.from(serialized).toString('base64');
    } catch (err) {
      this._logger.error(`createStakeTransaction failed: ${err}`);
      return '';
    }
  }

  async verifyStakeTransaction(txSignature: string, expectedAmount: bigint): Promise<boolean> {
    try {
      const tx = await this._connection.getTransaction(txSignature, {
        maxSupportedTransactionVersion: 0,
      });

      if (!tx || tx.meta?.err) {
        this._logger.warn(`TX ${txSignature} not found or failed`);
        return false;
      }

      // Verify the amount was transferred
      const preBalance = tx.meta?.preBalances?.[1] ?? 0;
      const postBalance = tx.meta?.postBalances?.[1] ?? 0;
      const transferred = BigInt(postBalance - preBalance);

      if (transferred >= expectedAmount) {
        this._logger.log(`Verified stake TX: ${txSignature}, amount=${transferred}`);
        return true;
      }

      this._logger.warn(`Stake amount mismatch: expected=${expectedAmount}, actual=${transferred}`);
      return false;
    } catch (err) {
      this._logger.error(`verifyStakeTransaction failed: ${err}`);
      return false;
    }
  }

  /**
   * Settle match: transfer winnings from treasury to winner.
   * 5% platform fee is retained in treasury.
   */
  async settleMatch(
    matchId: string,
    winnerAddress: string,
    totalAmount: bigint,
  ): Promise<string> {
    if (!this._authority) {
      this._logger.error('Cannot settle: no authority keypair');
      return '';
    }

    try {
      const winnerPubkey = new PublicKey(winnerAddress);
      const fee = totalAmount / 20n; // 5%
      const payout = totalAmount - fee;

      const transaction = new Transaction().add(
        SystemProgram.transfer({
          fromPubkey: this._authority.publicKey,
          toPubkey: winnerPubkey,
          lamports: Number(payout),
        }),
      );

      const signature = await sendAndConfirmTransaction(
        this._connection,
        transaction,
        [this._authority],
      );

      this._logger.log(`Settlement TX: match=${matchId}, winner=${winnerAddress}, payout=${payout}, fee=${fee}, sig=${signature}`);
      return signature;
    } catch (err) {
      this._logger.error(`settleMatch failed: ${err}`);
      return '';
    }
  }

  getExplorerUrl(txSignature: string): string {
    const cluster = this.network === 'mainnet-beta' ? '' : `?cluster=${this.network}`;
    return `https://explorer.solana.com/tx/${txSignature}${cluster}`;
  }
}
