using System;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    public class MainMenuController : UIScreenBase
    {
        public event Action OnPlayOffline;  // vs Friend (pass-and-play)
        public event Action OnPlayOnline;   // Ranked
        public event Action OnPlayBot;      // Casual (vs Bot)
        public event Action OnNavHeroes;
        public event Action OnNavRank;
        public event Action OnNavSettings;
        public event Action OnNavProfile;
        public event Action OnConnectWallet;

        private Button _btnPlay;
        private Button _btnRanked;
        private Button _btnCasual;
        private Button _btnFriend;
        private Label _heroName;
        private Label _heroRole;
        private Label _playerName;
        private Label _playerRank;

        private VisualElement _navHeroes;
        private VisualElement _navRank;
        private VisualElement _navSettings;
        private VisualElement _playerInfo;
        private Button _btnWallet;
        private Label _walletLabel;

        private enum PlayMode { Casual, Ranked, Friend }
        private PlayMode _playMode = PlayMode.Casual;

        protected override void QueryElements()
        {
            _btnPlay = Root.Q<Button>("btn-play");
            _btnRanked = Root.Q<Button>("btn-ranked");
            _btnCasual = Root.Q<Button>("btn-casual");
            _btnFriend = Root.Q<Button>("btn-friend");
            _heroName = Root.Q<Label>("hero-name");
            _heroRole = Root.Q<Label>("hero-role");
            _playerName = Root.Q<Label>("player-name");
            _playerRank = Root.Q<Label>("player-rank");

            _navHeroes = Root.Q("nav-heroes");
            _navRank = Root.Q("nav-rank");
            _navSettings = Root.Q("nav-settings");
            _playerInfo = _playerName?.parent;
            _btnWallet = Root.Q<Button>("btn-wallet");
            _walletLabel = Root.Q<Label>("wallet-label");
        }

        protected override void BindEvents()
        {
            _btnPlay?.RegisterCallback<ClickEvent>(HandlePlay);
            _btnRanked?.RegisterCallback<ClickEvent>(HandleRanked);
            _btnCasual?.RegisterCallback<ClickEvent>(HandleCasual);
            _btnFriend?.RegisterCallback<ClickEvent>(HandleFriend);

            _navHeroes?.RegisterCallback<ClickEvent>(HandleNavHeroes);
            _navRank?.RegisterCallback<ClickEvent>(HandleNavRank);
            _navSettings?.RegisterCallback<ClickEvent>(HandleNavSettings);
            _playerInfo?.RegisterCallback<ClickEvent>(HandleNavProfile);
            _btnWallet?.RegisterCallback<ClickEvent>(HandleConnectWallet);
        }

        protected override void UnbindEvents()
        {
            _btnPlay?.UnregisterCallback<ClickEvent>(HandlePlay);
            _btnRanked?.UnregisterCallback<ClickEvent>(HandleRanked);
            _btnCasual?.UnregisterCallback<ClickEvent>(HandleCasual);
            _btnFriend?.UnregisterCallback<ClickEvent>(HandleFriend);

            _navHeroes?.UnregisterCallback<ClickEvent>(HandleNavHeroes);
            _navRank?.UnregisterCallback<ClickEvent>(HandleNavRank);
            _navSettings?.UnregisterCallback<ClickEvent>(HandleNavSettings);
            _playerInfo?.UnregisterCallback<ClickEvent>(HandleNavProfile);
            _btnWallet?.UnregisterCallback<ClickEvent>(HandleConnectWallet);
        }

        public void SetPlayerInfo(string name, string rank)
        {
            if (_playerName != null) _playerName.text = name;
            if (_playerRank != null) _playerRank.text = rank;
        }

        public void SetHeroPreview(string heroName, string role)
        {
            if (_heroName != null) _heroName.text = heroName;
            if (_heroRole != null) _heroRole.text = role;
        }

        private void HandlePlay(ClickEvent _)
        {
            switch (_playMode)
            {
                case PlayMode.Ranked:  OnPlayOnline?.Invoke(); break;
                case PlayMode.Casual:  OnPlayBot?.Invoke(); break;
                case PlayMode.Friend:  OnPlayOffline?.Invoke(); break;
            }
        }

        private void HandleRanked(ClickEvent _)
        {
            _playMode = PlayMode.Ranked;
            UpdateModeButtons();
        }

        private void HandleCasual(ClickEvent _)
        {
            _playMode = PlayMode.Casual;
            UpdateModeButtons();
        }

        private void HandleFriend(ClickEvent _)
        {
            _playMode = PlayMode.Friend;
            UpdateModeButtons();
        }

        private void HandleNavHeroes(ClickEvent _) => OnNavHeroes?.Invoke();
        private void HandleNavRank(ClickEvent _) => OnNavRank?.Invoke();
        private void HandleNavSettings(ClickEvent _) => OnNavSettings?.Invoke();
        private void HandleNavProfile(ClickEvent _) => OnNavProfile?.Invoke();
        private void HandleConnectWallet(ClickEvent _) => OnConnectWallet?.Invoke();

        public void SetWalletStatus(bool connected, string address = null)
        {
            if (_walletLabel == null) return;
            if (connected && address != null)
            {
                _walletLabel.text = $"{address[..4]}..{address[^4..]}";
                _walletLabel.style.color = new StyleColor(new UnityEngine.Color(0.2f, 0.8f, 0.4f));
            }
            else
            {
                _walletLabel.text = "Wallet";
                _walletLabel.style.color = new StyleColor(new UnityEngine.Color(0.6f, 0.6f, 0.69f));
            }
        }

        private void UpdateModeButtons()
        {
            _btnRanked?.RemoveFromClassList("btn--tab-active");
            _btnCasual?.RemoveFromClassList("btn--tab-active");
            _btnFriend?.RemoveFromClassList("btn--tab-active");

            var activeBtn = _playMode switch
            {
                PlayMode.Ranked => _btnRanked,
                PlayMode.Friend => _btnFriend,
                _ => _btnCasual
            };
            activeBtn?.AddToClassList("btn--tab-active");
        }
    }
}
