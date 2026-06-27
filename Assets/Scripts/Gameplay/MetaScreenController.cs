using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TacticalDuelist.Core.Localization;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Platform;
using TacticalDuelist.UI.Toolkit;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Handles meta screens (Profile, Leaderboard, Settings, HeroesCollection, Wallet)
    /// that are independent of the core game flow state machine.
    /// Extracted from GameManager to reduce its size.
    /// </summary>
    public class MetaScreenController
    {
        private readonly UIManager _ui;
        private readonly Func<string> _getServerUrl;
        private readonly Action _returnToMainMenu;
        private readonly HeroPreview3D _heroPreview;
        private readonly HeroPreview3D _heroSelectPreview;

        public MetaScreenController(
            UIManager ui,
            Func<string> getServerUrl,
            Action returnToMainMenu,
            HeroPreview3D heroPreview,
            HeroPreview3D heroSelectPreview)
        {
            _ui = ui;
            _getServerUrl = getServerUrl;
            _returnToMainMenu = returnToMainMenu;
            _heroPreview = heroPreview;
            _heroSelectPreview = heroSelectPreview;
        }

        public void ShowHeroesCollection()
        {
            _heroPreview?.SetVisible(false);
            _heroSelectPreview?.SetVisible(false);
            if (_ui.HeroesCollection != null)
                _ui.ShowScreen(_ui.HeroesCollection);
        }

        public void ShowSettings()
        {
            _heroPreview?.SetVisible(false);
            if (_ui.Settings != null)
                _ui.ShowScreen(_ui.Settings);
        }

        public async void ShowLeaderboard()
        {
            _heroPreview?.SetVisible(false);
            if (_ui.Leaderboard == null) return;

            _ui.Leaderboard.SetLoading();
            _ui.ShowScreen(_ui.Leaderboard);

            try
            {
                var url = _getServerUrl();
                using var req = UnityEngine.Networking.UnityWebRequest.Get($"{url}/player/leaderboard");
                await req.SendWebRequest().ToUniTask();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var json = $"{{\"items\":{req.downloadHandler.text}}}";
                    var response = JsonUtility.FromJson<LeaderboardResponse>(json);
                    var list = new System.Collections.Generic.List<LeaderboardEntry>();
                    if (response?.items != null)
                        list.AddRange(response.items);
                    _ui.Leaderboard.SetData(list);
                }
                else
                {
                    _ui.Leaderboard.SetData(null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MetaScreens] Leaderboard fetch error: {ex.Message}");
                _ui.Leaderboard.SetData(null);
            }
        }

        public async void ShowProfile()
        {
            if (_ui.Profile == null) return;

            _ui.Profile.SetOfflineProfile("Loading...", 0, 0);
            _ui.ShowScreen(_ui.Profile);

            try
            {
                var auth = ServiceLocator.Get<IPlatformAuth>();
                var token = await auth.Authenticate();
                if (!string.IsNullOrEmpty(token))
                {
                    var url = _getServerUrl();
                    using var req = UnityEngine.Networking.UnityWebRequest.Get($"{url}/player/me");
                    req.SetRequestHeader("Authorization", $"Bearer {token}");
                    await req.SendWebRequest().ToUniTask();

                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<ServerProfileResponse>(req.downloadHandler.text);
                        _ui.Profile.SetProfile(response.ToProfileData());
                    }
                    else
                    {
                        Debug.LogWarning($"[MetaScreens] Profile fetch failed: {req.error}");
                    }

                    // Fetch match history
                    try
                    {
                        using var histReq = UnityEngine.Networking.UnityWebRequest.Get($"{url}/player/match-history");
                        histReq.SetRequestHeader("Authorization", $"Bearer {token}");
                        await histReq.SendWebRequest().ToUniTask();

                        if (histReq.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            var histJson = $"{{\"items\":{histReq.downloadHandler.text}}}";
                            var histResponse = JsonUtility.FromJson<MatchHistoryResponse>(histJson);
                            if (histResponse?.items != null)
                                _ui.Profile.SetMatchHistory(histResponse.items);
                        }
                    }
                    catch (Exception ex2)
                    {
                        Debug.LogWarning($"[MetaScreens] Match history fetch error: {ex2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MetaScreens] Profile fetch error: {ex.Message}");
            }
        }

        public async void HandleConnectWallet()
        {
            var blockchain = ServiceLocator.Get<IBlockchainService>();
            if (blockchain == null)
            {
                _ui.Toasts?.ShowToast(L.Get("blockchain_unavailable"), ToastType.Warning);
                return;
            }

            if (blockchain.IsConnected)
            {
                blockchain.DisconnectWallet();
                UpdateWalletUI();
                _ui.Toasts?.ShowToast(L.Get("wallet_disconnected"), ToastType.Info);
                return;
            }

            try
            {
                _ui.Toasts?.ShowToast(L.Get("connecting_wallet"), ToastType.Info);
                var address = await blockchain.ConnectWallet();
                if (!string.IsNullOrEmpty(address))
                {
                    UpdateWalletUI();
                    _ui.Toasts?.ShowToast(L.Get("wallet_connected", address[..6]), ToastType.Success);

                    try
                    {
                        var auth = ServiceLocator.Get<IPlatformAuth>();
                        var token = await auth.Authenticate();
                        if (!string.IsNullOrEmpty(token))
                        {
                            var url = _getServerUrl();
                            var body = $"{{\"walletAddress\":\"{address}\"}}";
                            using var req = new UnityEngine.Networking.UnityWebRequest($"{url}/blockchain/connect-wallet", "POST");
                            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                            req.SetRequestHeader("Content-Type", "application/json");
                            req.SetRequestHeader("Authorization", $"Bearer {token}");
                            await req.SendWebRequest().ToUniTask();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MetaScreens] Failed to save wallet to server: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _ui.Toasts?.ShowToast(L.Get("wallet_error", ex.Message), ToastType.Error);
            }
        }

        public void UpdateWalletUI()
        {
            var blockchain = ServiceLocator.Get<IBlockchainService>();
            if (_ui.MainMenu != null && blockchain != null)
                _ui.MainMenu.SetWalletStatus(blockchain.IsConnected, blockchain.WalletAddress);
        }

        public void WireMetaScreenBack(BackableScreenController screen)
        {
            if (screen == null) return;
            screen.OnBack -= _returnToMainMenu;
            screen.OnBack += _returnToMainMenu;
        }

        public void WireHeroesCollectionBack()
        {
            if (_ui.HeroesCollection != null)
            {
                _ui.HeroesCollection.OnBack -= _returnToMainMenu;
                _ui.HeroesCollection.OnBack += _returnToMainMenu;
            }
        }
    }
}
