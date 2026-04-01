var SolanaPlugin = {

    $SolanaState: {
        provider: null,
        publicKey: null,
        connected: false
    },

    // ── Connect Phantom Wallet ──
    Solana_ConnectWallet: function () {
        // Check for Phantom
        var provider = null;
        if (typeof window.phantom !== 'undefined' && window.phantom.solana) {
            provider = window.phantom.solana;
        } else if (typeof window.solana !== 'undefined' && window.solana.isPhantom) {
            provider = window.solana;
        }

        if (!provider) {
            // Try Phantom deep link for mobile
            var currentUrl = encodeURIComponent(window.location.href);
            window.open('https://phantom.app/ul/browse/' + currentUrl, '_blank');
            Module.SendMessage('WebGLBlockchainReceiver', 'OnWalletError', 'Phantom wallet not found. Please install Phantom browser extension or app.');
            return;
        }

        provider.connect()
            .then(function (resp) {
                SolanaState.provider = provider;
                SolanaState.publicKey = resp.publicKey.toString();
                SolanaState.connected = true;
                console.log('[SolanaPlugin] Connected:', SolanaState.publicKey);
                Module.SendMessage('WebGLBlockchainReceiver', 'OnWalletConnected', SolanaState.publicKey);
            })
            .catch(function (err) {
                console.error('[SolanaPlugin] Connection rejected:', err);
                Module.SendMessage('WebGLBlockchainReceiver', 'OnWalletError', err.message || 'Connection rejected by user');
            });
    },

    // ── Disconnect Wallet ──
    Solana_DisconnectWallet: function () {
        if (SolanaState.provider && SolanaState.provider.disconnect) {
            SolanaState.provider.disconnect();
        }
        SolanaState.publicKey = null;
        SolanaState.connected = false;
        console.log('[SolanaPlugin] Disconnected');
        Module.SendMessage('WebGLBlockchainReceiver', 'OnWalletDisconnected', '');
    },

    // ── Get Public Key ──
    Solana_GetPublicKey: function () {
        var key = SolanaState.publicKey || '';
        var bufferSize = lengthBytesUTF8(key) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(key, buffer, bufferSize);
        return buffer;
    },

    // ── Check if Connected ──
    Solana_IsConnected: function () {
        return SolanaState.connected ? 1 : 0;
    },

    // ── Get SOL Balance ──
    Solana_GetBalance: function () {
        if (!SolanaState.publicKey) {
            Module.SendMessage('WebGLBlockchainReceiver', 'OnBalanceReceived', '0');
            return;
        }

        // Use solanaWeb3 global from CDN
        if (typeof solanaWeb3 === 'undefined') {
            console.warn('[SolanaPlugin] solanaWeb3 not loaded');
            Module.SendMessage('WebGLBlockchainReceiver', 'OnBalanceReceived', '0');
            return;
        }

        var connection = new solanaWeb3.Connection('https://api.devnet.solana.com');
        var pubkey = new solanaWeb3.PublicKey(SolanaState.publicKey);

        connection.getBalance(pubkey)
            .then(function (balance) {
                Module.SendMessage('WebGLBlockchainReceiver', 'OnBalanceReceived', balance.toString());
            })
            .catch(function (err) {
                console.error('[SolanaPlugin] Balance error:', err);
                Module.SendMessage('WebGLBlockchainReceiver', 'OnBalanceReceived', '0');
            });
    },

    // ── Sign and Send Transaction ──
    // serializedTx is base64-encoded transaction from server
    Solana_SignAndSendTransaction: function (serializedTxPtr) {
        var base64Tx = UTF8ToString(serializedTxPtr);

        if (!SolanaState.provider || !SolanaState.connected) {
            Module.SendMessage('WebGLBlockchainReceiver', 'OnTransactionResult',
                JSON.stringify({ success: false, error: 'Wallet not connected' }));
            return;
        }

        if (typeof solanaWeb3 === 'undefined') {
            Module.SendMessage('WebGLBlockchainReceiver', 'OnTransactionResult',
                JSON.stringify({ success: false, error: 'Solana Web3 not loaded' }));
            return;
        }

        try {
            // Decode base64 transaction
            var txBytes = Uint8Array.from(atob(base64Tx), function(c) { return c.charCodeAt(0); });
            var transaction = solanaWeb3.Transaction.from(txBytes);

            // Sign with Phantom
            SolanaState.provider.signAndSendTransaction(transaction)
                .then(function (result) {
                    console.log('[SolanaPlugin] TX sent:', result.signature);
                    Module.SendMessage('WebGLBlockchainReceiver', 'OnTransactionResult',
                        JSON.stringify({ success: true, signature: result.signature }));
                })
                .catch(function (err) {
                    console.error('[SolanaPlugin] TX error:', err);
                    Module.SendMessage('WebGLBlockchainReceiver', 'OnTransactionResult',
                        JSON.stringify({ success: false, error: err.message || 'Transaction rejected' }));
                });
        } catch (e) {
            console.error('[SolanaPlugin] Parse error:', e);
            Module.SendMessage('WebGLBlockchainReceiver', 'OnTransactionResult',
                JSON.stringify({ success: false, error: e.message }));
        }
    }
};

autoAddDeps(SolanaPlugin, '$SolanaState');
mergeInto(LibraryManager.library, SolanaPlugin);
