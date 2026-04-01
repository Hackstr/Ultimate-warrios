using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Platform;

namespace TacticalDuelist.UI.Toolkit
{
    public class PreMatchController : UIScreenBase
    {
        protected override ScreenTransition Transition => ScreenTransition.SlideUp;
        public event Action OnStartMatch;
        public event Action OnBack;

        private Button _btnStart;
        private Button _btnBack;
        private Button _btnShrinkToggle;
        private Label _shrinkLabel;
        private Label _heroInitials;
        private Label _heroName;
        private Label _heroLore;
        private Label _modeLabel;
        private Label _opponentLabel;
        private Label _timerValue;
        private Label _walletStatus;
        private VisualElement _mapList;
        private VisualElement _stakeOptions;

        private HeroConfig _selectedHero;
        private MapConfig[] _maps;
        private int _selectedMapIndex;
        private bool _shrinkEnabled = true;
        private bool _isBot;
        private bool _isPassAndPlay;
        private int _selectedStake; // 0 = free, lamports otherwise

        public MapConfig SelectedMap => _maps != null && _selectedMapIndex >= 0 && _selectedMapIndex < _maps.Length
            ? _maps[_selectedMapIndex] : null;
        public bool ShrinkEnabled => _shrinkEnabled;
        public int SelectedStake => _selectedStake;

        protected override void QueryElements()
        {
            _btnStart = Root.Q<Button>("btn-start");
            _btnBack = Root.Q<Button>("btn-back");
            _btnShrinkToggle = Root.Q<Button>("btn-shrink-toggle");
            _shrinkLabel = Root.Q<Label>("shrink-label");
            _heroInitials = Root.Q<Label>("hero-initials");
            _heroName = Root.Q<Label>("hero-name");
            _heroLore = Root.Q<Label>("hero-lore");
            _modeLabel = Root.Q<Label>("mode-label");
            _opponentLabel = Root.Q<Label>("opponent-label");
            _timerValue = Root.Q<Label>("timer-value");
            _walletStatus = Root.Q<Label>("wallet-status");
            _mapList = Root.Q("map-list");
            _stakeOptions = Root.Q("stake-options");
        }

        protected override void BindEvents()
        {
            _btnStart?.RegisterCallback<ClickEvent>(HandleStart);
            _btnBack?.RegisterCallback<ClickEvent>(HandleBack);
            _btnShrinkToggle?.RegisterCallback<ClickEvent>(HandleShrinkToggle);
        }

        protected override void UnbindEvents()
        {
            _btnStart?.UnregisterCallback<ClickEvent>(HandleStart);
            _btnBack?.UnregisterCallback<ClickEvent>(HandleBack);
            _btnShrinkToggle?.UnregisterCallback<ClickEvent>(HandleShrinkToggle);
        }

        public void Show(HeroConfig hero, MapConfig[] maps, bool isBot, bool isPassAndPlay)
        {
            _selectedHero = hero;
            _maps = maps;
            _isBot = isBot;
            _isPassAndPlay = isPassAndPlay;
            _selectedMapIndex = -1; // random
            _shrinkEnabled = true;

            _selectedStake = 0;

            UpdateHeroInfo();
            UpdateModeInfo();
            UpdateShrinkToggle();
            BuildMapList();
            BuildStakeOptions();
            base.Show();
        }

        private void UpdateHeroInfo()
        {
            if (_selectedHero == null) return;

            if (_heroInitials != null)
            {
                string name = _selectedHero.displayName;
                _heroInitials.text = name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
                _heroInitials.style.color = new StyleColor(_selectedHero.heroColor);
            }

            if (_heroName != null)
                _heroName.text = _selectedHero.displayName;

            if (_heroLore != null)
            {
                string lore = !string.IsNullOrEmpty(_selectedHero.loreName)
                    ? $"{_selectedHero.loreName} -- {_selectedHero.loreTitle}"
                    : _selectedHero.specialDescription ?? "";
                _heroLore.text = lore;
            }
        }

        private void UpdateModeInfo()
        {
            if (_modeLabel != null)
            {
                if (_isBot) _modeLabel.text = "Casual (vs Bot)";
                else if (_isPassAndPlay) _modeLabel.text = "vs Friend (Local)";
                else _modeLabel.text = "Ranked (Online)";
            }

            if (_opponentLabel != null)
            {
                if (_isBot) _opponentLabel.text = "Smart Bot";
                else if (_isPassAndPlay) _opponentLabel.text = "Player 2";
                else _opponentLabel.text = "Matchmaking...";
            }

            if (_timerValue != null)
                _timerValue.text = "30s";
        }

        private void BuildMapList()
        {
            if (_mapList == null || _maps == null) return;
            _mapList.UnregisterCallback<ClickEvent>(HandleMapClick);
            _mapList.Clear();

            // Random option
            var randomCard = CreateMapCard("Random Arena", "A random arena will be selected", -1);
            _mapList.Add(randomCard);
            SelectMapCard(randomCard); // default

            for (int i = 0; i < _maps.Length; i++)
            {
                var map = _maps[i];
                string desc = $"{map.width}x{map.height}";
                var card = CreateMapCard(map.mapName, desc, i);
                _mapList.Add(card);
            }

            _mapList.RegisterCallback<ClickEvent>(HandleMapClick);
        }

        private VisualElement CreateMapCard(string name, string desc, int index)
        {
            var card = new VisualElement();
            card.userData = index;
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.borderTopLeftRadius = 16;
            card.style.borderTopRightRadius = 16;
            card.style.borderBottomLeftRadius = 16;
            card.style.borderBottomRightRadius = 16;
            card.style.marginBottom = 8;
            card.style.backgroundColor = new Color(0.13f, 0.13f, 0.22f);

            // Map icon
            var icon = new VisualElement();
            icon.style.width = 48;
            icon.style.height = 48;
            icon.style.borderTopLeftRadius = 12;
            icon.style.borderTopRightRadius = 12;
            icon.style.borderBottomLeftRadius = 12;
            icon.style.borderBottomRightRadius = 12;
            icon.style.backgroundColor = new Color(0.16f, 0.16f, 0.25f);
            icon.style.marginRight = 16;
            icon.style.alignItems = Align.Center;
            icon.style.justifyContent = Justify.Center;

            var iconLabel = new Label(index < 0 ? "?" : (index + 1).ToString());
            iconLabel.style.fontSize = 22;
            iconLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.69f));
            iconLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            iconLabel.pickingMode = PickingMode.Ignore;
            icon.Add(iconLabel);
            card.Add(icon);

            // Text
            var col = new VisualElement();
            col.style.flexGrow = 1;
            col.style.flexDirection = FlexDirection.Column;

            var nameLabel = new Label(name);
            nameLabel.style.fontSize = 28;
            nameLabel.style.color = new StyleColor(new Color(0.92f, 0.92f, 0.96f));
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.pickingMode = PickingMode.Ignore;
            col.Add(nameLabel);

            var descLabel = new Label(desc);
            descLabel.style.fontSize = 20;
            descLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.69f));
            descLabel.style.marginTop = 2;
            descLabel.pickingMode = PickingMode.Ignore;
            col.Add(descLabel);

            card.Add(col);
            return card;
        }

        private void HandleMapClick(ClickEvent evt)
        {
            var el = evt.target as VisualElement;
            while (el != null && el != _mapList)
            {
                if (el.userData is int idx)
                {
                    _selectedMapIndex = idx;
                    SelectMapCard(el);
                    return;
                }
                el = el.parent;
            }
        }

        private void SelectMapCard(VisualElement selected)
        {
            if (_mapList == null) return;
            foreach (var child in _mapList.Children())
            {
                bool active = child == selected;
                child.style.backgroundColor = active
                    ? new Color(0.1f, 0.08f, 0.05f)
                    : new Color(0.13f, 0.13f, 0.22f);
                child.style.borderTopWidth = active ? 2 : 0;
                child.style.borderRightWidth = active ? 2 : 0;
                child.style.borderBottomWidth = active ? 2 : 0;
                child.style.borderLeftWidth = active ? 2 : 0;
                var borderColor = new StyleColor(new Color(1f, 0.42f, 0.21f, active ? 0.6f : 0f));
                child.style.borderTopColor = borderColor;
                child.style.borderRightColor = borderColor;
                child.style.borderBottomColor = borderColor;
                child.style.borderLeftColor = borderColor;
            }
        }

        private void HandleShrinkToggle(ClickEvent _)
        {
            _shrinkEnabled = !_shrinkEnabled;
            UpdateShrinkToggle();
        }

        private void UpdateShrinkToggle()
        {
            if (_btnShrinkToggle != null)
                _btnShrinkToggle.style.backgroundColor = _shrinkEnabled
                    ? new StyleColor(new Color(0.2f, 0.8f, 0.4f))
                    : new StyleColor(new Color(0.3f, 0.3f, 0.4f));

            if (_shrinkLabel != null)
                _shrinkLabel.text = _shrinkEnabled ? "ON" : "OFF";
        }

        private void BuildStakeOptions()
        {
            if (_stakeOptions == null) return;
            _stakeOptions.Clear();

            // Check wallet connection
            var blockchain = Platform.ServiceLocator.Get<IBlockchainService>();
            bool walletConnected = blockchain != null && blockchain.IsConnected;

            if (_walletStatus != null)
            {
                _walletStatus.text = walletConnected
                    ? $"Wallet: {blockchain.WalletAddress[..6]}...{blockchain.WalletAddress[^4..]}"
                    : "No wallet connected";
                _walletStatus.style.color = walletConnected
                    ? new StyleColor(new Color(0.2f, 0.8f, 0.4f))
                    : new StyleColor(new Color(0.6f, 0.6f, 0.69f));
            }

            // Stake options
            var stakes = new[] { (0, "FREE"), (10_000_000, "0.01"), (50_000_000, "0.05"), (100_000_000, "0.1") };
            foreach (var (amount, label) in stakes)
            {
                bool isSelected = _selectedStake == amount;
                bool isEnabled = amount == 0 || walletConnected;

                var btn = new VisualElement();
                btn.userData = amount;
                btn.style.flexGrow = 1;
                btn.style.height = 48;
                btn.style.borderTopLeftRadius = 12;
                btn.style.borderTopRightRadius = 12;
                btn.style.borderBottomLeftRadius = 12;
                btn.style.borderBottomRightRadius = 12;
                btn.style.alignItems = Align.Center;
                btn.style.justifyContent = Justify.Center;
                btn.style.marginRight = 6;
                btn.style.opacity = isEnabled ? 1f : 0.35f;

                if (isSelected)
                {
                    btn.style.backgroundColor = new Color(0.1f, 0.08f, 0.05f);
                    btn.style.borderTopWidth = 2;
                    btn.style.borderRightWidth = 2;
                    btn.style.borderBottomWidth = 2;
                    btn.style.borderLeftWidth = 2;
                    var bc = new StyleColor(new Color(1f, 0.42f, 0.21f));
                    btn.style.borderTopColor = bc;
                    btn.style.borderRightColor = bc;
                    btn.style.borderBottomColor = bc;
                    btn.style.borderLeftColor = bc;
                }
                else
                {
                    btn.style.backgroundColor = new Color(0.1f, 0.1f, 0.16f);
                }

                var text = new Label(amount == 0 ? "FREE" : $"{label} SOL");
                text.style.fontSize = amount == 0 ? 16 : 14;
                text.style.unityFontStyleAndWeight = FontStyle.Bold;
                text.style.color = isSelected
                    ? new StyleColor(new Color(1f, 0.42f, 0.21f))
                    : new StyleColor(new Color(0.75f, 0.75f, 0.82f));
                text.style.unityTextAlign = TextAnchor.MiddleCenter;
                text.pickingMode = PickingMode.Ignore;
                btn.Add(text);

                _stakeOptions.Add(btn);
            }

            _stakeOptions.RegisterCallback<ClickEvent>(HandleStakeClick);
        }

        private void HandleStakeClick(ClickEvent evt)
        {
            var el = evt.target as VisualElement;
            while (el != null && el != _stakeOptions)
            {
                if (el.userData is int amount)
                {
                    _selectedStake = amount;
                    BuildStakeOptions(); // Rebuild to update selection
                    return;
                }
                el = el.parent;
            }
        }

        private void HandleStart(ClickEvent _) => OnStartMatch?.Invoke();
        private void HandleBack(ClickEvent _)
        {
            Debug.Log($"[PreMatch] Back button clicked! OnBack subscribers: {OnBack?.GetInvocationList()?.Length ?? 0}");
            OnBack?.Invoke();
        }
    }
}
