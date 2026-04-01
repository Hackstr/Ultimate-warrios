using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.UI.Toolkit
{
    public class HeroesCollectionController : UIScreenBase
    {
        protected override ScreenTransition Transition => ScreenTransition.SlideLeft;
        public event Action OnBack;
        public event Action<HeroConfig> OnUnlockRequested;

        private Button _btnBack;
        private VisualElement _heroesGrid;
        private Label _heroCount;
        private Label _coinsLabel;
        private List<HeroConfig> _heroes;
        private int _playerCoins;

        protected override void QueryElements()
        {
            _btnBack = Root.Q<Button>("btn-back");
            _heroesGrid = Root.Q("heroes-grid");
            _heroCount = Root.Q<Label>("hero-count");
            _coinsLabel = Root.Q<Label>("coins-label");
        }

        protected override void BindEvents()
        {
            _btnBack?.RegisterCallback<ClickEvent>(HandleBack);
        }

        protected override void UnbindEvents()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(HandleBack);
        }

        public void SetHeroes(List<HeroConfig> heroes) => _heroes = heroes;

        public void SetCoins(int coins)
        {
            _playerCoins = coins;
            if (_coinsLabel != null) _coinsLabel.text = $"{coins} coins";
        }

        protected override void OnShow() => BuildGrid();

        private void BuildGrid()
        {
            if (_heroesGrid == null || _heroes == null) return;
            _heroesGrid.UnregisterCallback<ClickEvent>(HandleCardClick);
            _heroesGrid.Clear();

            int unlocked = 0;
            foreach (var hero in _heroes)
            {
                if (string.IsNullOrEmpty(hero.heroId)) continue;

                bool isUnlocked = hero.isUnlocked;
                if (isUnlocked) unlocked++;

                // Card: ~31% width (3 per row with gaps)
                var card = new VisualElement();
                card.userData = hero;
                card.style.width = Length.Percent(30);
                card.style.height = 200;
                card.style.borderTopLeftRadius = 20;
                card.style.borderTopRightRadius = 20;
                card.style.borderBottomLeftRadius = 20;
                card.style.borderBottomRightRadius = 20;
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.Center;
                card.style.flexDirection = FlexDirection.Column;
                card.style.marginRight = Length.Percent(1.5f);
                card.style.marginBottom = 12;
                card.style.paddingTop = 16;
                card.style.paddingBottom = 12;

                if (isUnlocked)
                {
                    card.style.backgroundColor = new Color(hero.heroColor.r * 0.12f, hero.heroColor.g * 0.12f, hero.heroColor.b * 0.12f);
                    card.style.borderTopWidth = 2;
                    card.style.borderRightWidth = 2;
                    card.style.borderBottomWidth = 2;
                    card.style.borderLeftWidth = 2;
                    var borderColor = new Color(hero.heroColor.r, hero.heroColor.g, hero.heroColor.b, 0.4f);
                    card.style.borderTopColor = borderColor;
                    card.style.borderRightColor = borderColor;
                    card.style.borderBottomColor = borderColor;
                    card.style.borderLeftColor = borderColor;
                }
                else
                {
                    card.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
                    card.style.opacity = 0.55f;
                }

                // Initials circle
                var circle = new VisualElement();
                circle.style.width = 64;
                circle.style.height = 64;
                circle.style.borderTopLeftRadius = 32;
                circle.style.borderTopRightRadius = 32;
                circle.style.borderBottomLeftRadius = 32;
                circle.style.borderBottomRightRadius = 32;
                circle.style.alignItems = Align.Center;
                circle.style.justifyContent = Justify.Center;
                circle.style.backgroundColor = isUnlocked
                    ? new Color(hero.heroColor.r * 0.2f, hero.heroColor.g * 0.2f, hero.heroColor.b * 0.2f)
                    : new Color(0.12f, 0.12f, 0.18f);

                var initials = new Label(isUnlocked ? hero.displayName[..2].ToUpper() : "?");
                initials.style.fontSize = 28;
                initials.style.unityFontStyleAndWeight = FontStyle.Bold;
                initials.style.color = isUnlocked
                    ? new StyleColor(hero.heroColor)
                    : new StyleColor(new Color(0.35f, 0.35f, 0.45f));
                initials.style.unityTextAlign = TextAnchor.MiddleCenter;
                initials.pickingMode = PickingMode.Ignore;
                circle.Add(initials);
                card.Add(circle);

                // Lore name or display name
                string nameText = isUnlocked && !string.IsNullOrEmpty(hero.loreName)
                    ? hero.loreName
                    : hero.displayName;
                var nameLabel = new Label(nameText);
                nameLabel.style.fontSize = 24;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                nameLabel.style.marginTop = 8;
                nameLabel.style.color = isUnlocked
                    ? new StyleColor(hero.heroColor)
                    : new StyleColor(new Color(0.4f, 0.4f, 0.5f));
                nameLabel.pickingMode = PickingMode.Ignore;
                card.Add(nameLabel);

                // Role or price
                string subText;
                if (isUnlocked)
                    subText = hero.displayName;
                else if (hero.unlockPrice > 0)
                    subText = $"{hero.unlockPrice} coins";
                else
                    subText = "Locked";

                var subLabel = new Label(subText);
                subLabel.style.fontSize = 18;
                subLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                subLabel.style.marginTop = 2;
                subLabel.style.color = !isUnlocked && hero.unlockPrice > 0 && _playerCoins >= hero.unlockPrice
                    ? new StyleColor(new Color(1f, 0.42f, 0.21f))
                    : new StyleColor(new Color(0.5f, 0.5f, 0.58f));
                subLabel.pickingMode = PickingMode.Ignore;
                card.Add(subLabel);

                _heroesGrid.Add(card);
            }

            // Single click handler
            _heroesGrid.RegisterCallback<ClickEvent>(HandleCardClick);

            if (_heroCount != null)
                _heroCount.text = $"{unlocked}/{_heroes.Count}";
        }

        private void HandleCardClick(ClickEvent evt)
        {
            var el = evt.target as VisualElement;
            while (el != null && el != _heroesGrid)
            {
                if (el.userData is HeroConfig hero && !hero.isUnlocked && hero.unlockPrice > 0)
                {
                    OnUnlockRequested?.Invoke(hero);
                    return;
                }
                el = el.parent;
            }
        }

        private void HandleBack(ClickEvent _) => OnBack?.Invoke();
    }
}
