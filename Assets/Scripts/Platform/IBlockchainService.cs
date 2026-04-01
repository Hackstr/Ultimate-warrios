using System;
using Cysharp.Threading.Tasks;

namespace TacticalDuelist.Platform
{
    /// <summary>
    /// Blockchain abstraction layer. Supports Solana (hackathon/standalone)
    /// and TON (Telegram Mini App) through platform-specific implementations.
    /// </summary>
    public interface IBlockchainService
    {
        /// <summary>Current blockchain network name (e.g. "solana-devnet", "ton-testnet").</summary>
        string NetworkName { get; }

        /// <summary>Whether a wallet is currently connected.</summary>
        bool IsConnected { get; }

        /// <summary>Connected wallet address (null if not connected).</summary>
        string WalletAddress { get; }

        /// <summary>Connect wallet (Phantom for Solana, TON Connect for TON).</summary>
        UniTask<string> ConnectWallet();

        /// <summary>Disconnect wallet.</summary>
        void DisconnectWallet();

        /// <summary>Get native token balance (SOL or TON) in smallest unit.</summary>
        UniTask<long> GetBalance();

        /// <summary>Get token balance for game token (SPL or Jetton).</summary>
        UniTask<long> GetGameTokenBalance();

        /// <summary>
        /// Request user to sign and send a stake transaction.
        /// Returns transaction signature/hash.
        /// </summary>
        UniTask<string> StakeForMatch(string matchId, long amount);

        /// <summary>
        /// Get transaction status and explorer URL.
        /// </summary>
        string GetExplorerUrl(string txSignature);

        /// <summary>Fired when wallet connects successfully.</summary>
        event Action<string> OnWalletConnected;

        /// <summary>Fired when wallet disconnects.</summary>
        event Action OnWalletDisconnected;
    }

    /// <summary>
    /// Result of a blockchain transaction.
    /// </summary>
    public struct BlockchainTxResult
    {
        public bool Success;
        public string TxSignature;
        public string Error;
        public string ExplorerUrl;
    }
}
