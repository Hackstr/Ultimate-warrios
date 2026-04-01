using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TacticalDuelist.Platform.Editor
{
    /// <summary>
    /// Mock blockchain for Editor testing. Logs operations and simulates
    /// wallet connection with a fake devnet address.
    /// </summary>
    public class EditorBlockchain : IBlockchainService
    {
        private string _walletAddress;
        private long _mockBalance = 5_000_000_000; // 5 SOL in lamports

        public string NetworkName => "solana-devnet (mock)";
        public bool IsConnected => _walletAddress != null;
        public string WalletAddress => _walletAddress;

        public event Action<string> OnWalletConnected;
        public event Action OnWalletDisconnected;

        public async UniTask<string> ConnectWallet()
        {
            // Simulate wallet connection delay
            await UniTask.Delay(500);

            _walletAddress = "EditorWallet_" + Guid.NewGuid().ToString("N")[..8];
            Debug.Log($"[EditorBlockchain] Wallet connected: {_walletAddress}");
            OnWalletConnected?.Invoke(_walletAddress);
            return _walletAddress;
        }

        public void DisconnectWallet()
        {
            Debug.Log($"[EditorBlockchain] Wallet disconnected: {_walletAddress}");
            _walletAddress = null;
            OnWalletDisconnected?.Invoke();
        }

        public async UniTask<long> GetBalance()
        {
            await UniTask.Delay(100);
            Debug.Log($"[EditorBlockchain] Balance: {_mockBalance} lamports ({_mockBalance / 1_000_000_000f:F2} SOL)");
            return _mockBalance;
        }

        public async UniTask<long> GetGameTokenBalance()
        {
            await UniTask.Delay(100);
            return 0; // No game token on mock
        }

        public async UniTask<string> StakeForMatch(string matchId, long amount)
        {
            await UniTask.Delay(300);

            if (amount > _mockBalance)
            {
                Debug.LogWarning($"[EditorBlockchain] Insufficient balance for stake: {amount} > {_mockBalance}");
                return null;
            }

            _mockBalance -= amount;
            string txSig = "mock_tx_" + Guid.NewGuid().ToString("N")[..12];
            Debug.Log($"[EditorBlockchain] Staked {amount} lamports for match {matchId}. TX: {txSig}. Remaining: {_mockBalance}");
            return txSig;
        }

        public string GetExplorerUrl(string txSignature)
        {
            return $"https://explorer.solana.com/tx/{txSignature}?cluster=devnet";
        }
    }
}
