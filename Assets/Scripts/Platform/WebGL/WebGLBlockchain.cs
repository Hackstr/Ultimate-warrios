using System;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TacticalDuelist.Platform.WebGL
{
#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// WebGL blockchain implementation using SolanaPlugin.jslib.
    /// Communicates with Phantom wallet via JavaScript bridge.
    /// </summary>
    public class WebGLBlockchain : IBlockchainService
    {
        [DllImport("__Internal")] private static extern void Solana_ConnectWallet();
        [DllImport("__Internal")] private static extern void Solana_DisconnectWallet();
        [DllImport("__Internal")] private static extern string Solana_GetPublicKey();
        [DllImport("__Internal")] private static extern int Solana_IsConnected();
        [DllImport("__Internal")] private static extern void Solana_GetBalance();
        [DllImport("__Internal")] private static extern void Solana_SignAndSendTransaction(string serializedTx);

        private string _walletAddress;
        private UniTaskCompletionSource<string> _connectTcs;
        private UniTaskCompletionSource<long> _balanceTcs;
        private UniTaskCompletionSource<string> _txTcs;

        public string NetworkName => "solana-devnet";
        public bool IsConnected
        {
            get
            {
                try { return Solana_IsConnected() == 1; }
                catch { return false; }
            }
        }
        public string WalletAddress => _walletAddress;

        public event Action<string> OnWalletConnected;
        public event Action OnWalletDisconnected;

        public void Initialize()
        {
            var receiver = WebGLBlockchainReceiver.Instance;
            if (receiver == null) return;

            receiver.WalletConnected += HandleConnected;
            receiver.WalletError += HandleError;
            receiver.WalletDisconnected += HandleDisconnected;
            receiver.BalanceReceived += HandleBalance;
            receiver.TransactionResult += HandleTxResult;
        }

        public async UniTask<string> ConnectWallet()
        {
            _connectTcs = new UniTaskCompletionSource<string>();
            Solana_ConnectWallet();

            try
            {
                _walletAddress = await _connectTcs.Task;
                return _walletAddress;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLBlockchain] Connect failed: {ex.Message}");
                return null;
            }
        }

        public void DisconnectWallet()
        {
            Solana_DisconnectWallet();
            _walletAddress = null;
        }

        public async UniTask<long> GetBalance()
        {
            _balanceTcs = new UniTaskCompletionSource<long>();
            Solana_GetBalance();
            return await _balanceTcs.Task;
        }

        public async UniTask<long> GetGameTokenBalance()
        {
            // TODO: SPL token balance query
            return 0;
        }

        public async UniTask<string> StakeForMatch(string matchId, long amount)
        {
            if (!IsConnected || _walletAddress == null)
                throw new Exception("Wallet not connected");

            // 1. Request serialized transaction from server
            var serverUrl = WebGLConfig.GetServerUrl();
            var body = $"{{\"matchId\":\"{matchId}\",\"walletAddress\":\"{_walletAddress}\",\"amount\":\"{amount}\"}}";

            using var req = new UnityEngine.Networking.UnityWebRequest($"{serverUrl}/blockchain/create-stake", "POST");
            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // Get auth token
            var auth = ServiceLocator.Get<IPlatformAuth>();
            if (auth != null)
            {
                var token = await auth.Authenticate();
                if (!string.IsNullOrEmpty(token))
                    req.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            await req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                throw new Exception($"Server error: {req.error}");

            var response = JsonUtility.FromJson<CreateStakeResponse>(req.downloadHandler.text);
            if (string.IsNullOrEmpty(response.transaction))
                throw new Exception("Server returned empty transaction");

            // 2. Sign with Phantom
            _txTcs = new UniTaskCompletionSource<string>();
            Solana_SignAndSendTransaction(response.transaction);

            return await _txTcs.Task;
        }

        [Serializable]
        private class CreateStakeResponse { public string transaction; }

        public string GetExplorerUrl(string txSignature)
        {
            return $"https://explorer.solana.com/tx/{txSignature}?cluster=devnet";
        }

        // ── JS Callback Handlers ──

        private void HandleConnected(string publicKey)
        {
            _walletAddress = publicKey;
            _connectTcs?.TrySetResult(publicKey);
            OnWalletConnected?.Invoke(publicKey);
        }

        private void HandleError(string error)
        {
            _connectTcs?.TrySetException(new Exception(error));
        }

        private void HandleDisconnected()
        {
            _walletAddress = null;
            OnWalletDisconnected?.Invoke();
        }

        private void HandleBalance(string lamports)
        {
            long.TryParse(lamports, out long balance);
            _balanceTcs?.TrySetResult(balance);
        }

        private void HandleTxResult(string json)
        {
            // Parse {success, signature, error}
            try
            {
                var result = JsonUtility.FromJson<TxResultJson>(json);
                if (result.success)
                    _txTcs?.TrySetResult(result.signature);
                else
                    _txTcs?.TrySetException(new Exception(result.error));
            }
            catch
            {
                _txTcs?.TrySetException(new Exception("Failed to parse tx result"));
            }
        }

        [Serializable]
        private class TxResultJson
        {
            public bool success;
            public string signature;
            public string error;
        }
    }
#endif
}
