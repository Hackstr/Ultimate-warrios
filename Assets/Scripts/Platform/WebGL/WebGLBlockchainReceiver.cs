using UnityEngine;

namespace TacticalDuelist.Platform.WebGL
{
    /// <summary>
    /// Receives callbacks from SolanaPlugin.jslib via Module.SendMessage.
    /// Created by PlatformBootstrap, lives as DontDestroyOnLoad.
    /// </summary>
    public class WebGLBlockchainReceiver : MonoBehaviour
    {
        public static WebGLBlockchainReceiver Instance { get; private set; }

        // Events fired by JS callbacks
        public event System.Action<string> WalletConnected;
        public event System.Action<string> WalletError;
        public event System.Action WalletDisconnected;
        public event System.Action<string> BalanceReceived;
        public event System.Action<string> TransactionResult;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Called from JS: Module.SendMessage('WebGLBlockchainReceiver', 'OnWalletConnected', publicKey)
        public void OnWalletConnected(string publicKey)
        {
            Debug.Log($"[WebGLBlockchain] Wallet connected: {publicKey}");
            WalletConnected?.Invoke(publicKey);
        }

        public void OnWalletError(string error)
        {
            Debug.LogError($"[WebGLBlockchain] Wallet error: {error}");
            WalletError?.Invoke(error);
        }

        public void OnWalletDisconnected(string _)
        {
            Debug.Log("[WebGLBlockchain] Wallet disconnected");
            WalletDisconnected?.Invoke();
        }

        public void OnBalanceReceived(string lamports)
        {
            BalanceReceived?.Invoke(lamports);
        }

        public void OnTransactionResult(string json)
        {
            TransactionResult?.Invoke(json);
        }
    }
}
