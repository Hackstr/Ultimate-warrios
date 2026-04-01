using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.UI.Toolkit
{
    public class HeroSelectController : UIScreenBase
    {
        protected override ScreenTransition Transition => ScreenTransition.SlideUp;
        public event Action<HeroConfig, HeroConfig> OnHeroesSelected;
        public event Action OnBackPressed;
        public event Action<HeroConfig> OnHeroChanged;

        private Button _btnSelect;
        private Button _btnBack;
        private VisualElement _rosterGrid;
        private VisualElement _heroPreview;
        private Label _heroName;
        private Label _subtitle;
        private VisualElement _statsContainer;
        private Label _abilityName;
        private Label _abilityDesc;

        private List<HeroConfig> _heroes;
        private HeroConfig _selectedHero;
        private bool _isPassAndPlay;
        private bool _isP2Selecting;
        private HeroConfig _p1Hero;

        protected override void QueryElements()
        {
            _btnSelect = Root.Q<Button>("btn-select");
            _btnBack = Root.Q<Button>("btn-back");
            _rosterGrid = Root.Q("roster-grid");
            _heroPreview = Root.Q("hero-preview");
            _heroName = Root.Q<Label>("hero-name");
            _subtitle = Root.Q<Label>("hero-select-subtitle");
            _statsContainer = Root.Q("hero-stats");
            _abilityName = Root.Q<Label>("ability-name");
            _abilityDesc = Root.Q<Label>("ability-desc");
        }

        protected override void BindEvents()
        {
            _btnSelect?.RegisterCallback<ClickEvent>(HandleSelect);
            _btnBack?.RegisterCallback<ClickEvent>(HandleBack);
        }

        protected override void UnbindEvents()
        {
            _btnSelect?.UnregisterCallback<ClickEvent>(HandleSelect);
            _btnBack?.UnregisterCallback<ClickEvent>(HandleBack);
        }

        public void SetHeroes(List<HeroConfig> heroes) => _heroes = heroes;
        public List<HeroConfig> GetHeroes() => _heroes;

        public void Show(bool isPassAndPlay)
        {
            _isPassAndPlay = isPassAndPlay;
            _isP2Selecting = false;
            _selectedHero = null;
            _p1Hero = null;
            base.Show();
            UpdateTitle();
            BuildRoster();

            // Select first unlocked hero
            if (_heroes != null)
            {
                foreach (var h in _heroes)
                {
                    if (h.isUnlocked)
                    {
                        SelectHero(h);
                        break;
                    }
                }
            }
        }

        public void ShowForP2()
        {
            _isP2Selecting = true;
            _selectedHero = null;
            UpdateTitle();

            if (_rosterGrid != null)
                foreach (var child in _rosterGrid.Children())
                    child.RemoveFromClassList("hero-card--selected");

            if (_heroes != null)
                foreach (var h in _heroes)
                    if (h.isUnlocked) { SelectHero(h); break; }
        }

        #region Roster Building

        private void BuildRoster()
        {
            if (_rosterGrid == null || _heroes == null) return;
            _rosterGrid.UnregisterCallback<ClickEvent>(HandleGridCardClick);
            _rosterGrid.Clear();

            foreach (var hero in _heroes)
            {
                if (string.IsNullOrEmpty(hero.heroId)) continue;

                bool locked = !hero.isUnlocked;
                var card = new VisualElement();
                card.userData = hero;

                // Horizontal card: 128x128 square with color and initials
                card.style.width = 128;
                card.style.height = 128;
                card.style.borderTopLeftRadius = 24;
                card.style.borderTopRightRadius = 24;
                card.style.borderBottomLeftRadius = 24;
                card.style.borderBottomRightRadius = 24;
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.Center;
                card.style.flexDirection = FlexDirection.Column;
                card.style.marginRight = 12;
                card.style.flexShrink = 0;

                if (locked)
                {
                    card.style.backgroundColor = new Color(0.1f, 0.1f, 0.16f);
                    card.style.opacity = 0.4f;
                }
                else
                {
                    card.style.backgroundColor = new Color(hero.heroColor.r * 0.15f, hero.heroColor.g * 0.15f, hero.heroColor.b * 0.15f);
                    card.style.borderTopWidth = 2;
                    card.style.borderRightWidth = 2;
                    card.style.borderBottomWidth = 2;
                    card.style.borderLeftWidth = 2;
                    card.style.borderTopColor = new Color(hero.heroColor.r, hero.heroColor.g, hero.heroColor.b, 0.3f);
                    card.style.borderRightColor = card.style.borderTopColor;
                    card.style.borderBottomColor = card.style.borderTopColor;
                    card.style.borderLeftColor = card.style.borderTopColor;
                }

                // Initials or lock
                var initials = new Label(locked ? "?" : (hero.displayName.Length >= 2 ? hero.displayName[..2].ToUpper() : hero.displayName.ToUpper()));
                initials.pickingMode = PickingMode.Ignore;
                initials.style.fontSize = locked ? 32 : 36;
                initials.style.unityFontStyleAndWeight = FontStyle.Bold;
                initials.style.unityTextAlign = TextAnchor.MiddleCenter;
                initials.style.color = locked
                    ? new StyleColor(new Color(0.4f, 0.4f, 0.5f))
                    : new StyleColor(hero.heroColor);
                card.Add(initials);

                // Name below initials
                var nameLabel = new Label(locked ? "Locked" : hero.displayName);
                nameLabel.pickingMode = PickingMode.Ignore;
                nameLabel.style.fontSize = 18;
                nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                nameLabel.style.marginTop = 4;
                nameLabel.style.color = locked
                    ? new StyleColor(new Color(0.4f, 0.4f, 0.5f))
                    : new StyleColor(new Color(0.75f, 0.75f, 0.82f));
                card.Add(nameLabel);

                card.AddToClassList("hero-card");
                _rosterGrid.Add(card);
            }

            _rosterGrid.RegisterCallback<ClickEvent>(HandleGridCardClick);
        }

        #endregion

        #region Selection

        private void HandleGridCardClick(ClickEvent evt)
        {
            var el = evt.target as VisualElement;
            while (el != null && el != _rosterGrid)
            {
                if (el.userData is HeroConfig hero)
                {
                    SelectHero(hero);
                    return;
                }
                el = el.parent;
            }
        }

        private void SelectHero(HeroConfig hero)
        {
            if (hero == null || !hero.isUnlocked) return;
            _selectedHero = hero;

            // Highlight selected card
            if (_rosterGrid != null)
            {
                foreach (var child in _rosterGrid.Children())
                {
                    if (child.userData is HeroConfig h && h == hero)
                    {
                        child.AddToClassList("hero-card--selected");
                        // Stronger border for selected
                        child.style.borderTopColor = new StyleColor(new Color(1f, 0.42f, 0.21f));
                        child.style.borderRightColor = child.style.borderTopColor;
                        child.style.borderBottomColor = child.style.borderTopColor;
                        child.style.borderLeftColor = child.style.borderTopColor;
                        child.style.borderTopWidth = 3;
                        child.style.borderRightWidth = 3;
                        child.style.borderBottomWidth = 3;
                        child.style.borderLeftWidth = 3;
                    }
                    else
                    {
                        child.RemoveFromClassList("hero-card--selected");
                        if (child.userData is HeroConfig oh && oh.isUnlocked)
                        {
                            var c = new Color(oh.heroColor.r, oh.heroColor.g, oh.heroColor.b, 0.3f);
                            child.style.borderTopColor = new StyleColor(c);
                            child.style.borderRightColor = new StyleColor(c);
                            child.style.borderBottomColor = new StyleColor(c);
                            child.style.borderLeftColor = new StyleColor(c);
                            child.style.borderTopWidth = 2;
                            child.style.borderRightWidth = 2;
                            child.style.borderBottomWidth = 2;
                            child.style.borderLeftWidth = 2;
                        }
                    }
                }
            }

            // Hero name (lore name if available)
            if (_heroName != null)
            {
                string name = !string.IsNullOrEmpty(hero.loreName)
                    ? $"{hero.loreName.ToUpper()} — {hero.displayName}"
                    : hero.displayName.ToUpper();
                _heroName.text = name;
            }

            // Ability
            if (_abilityName != null)
                _abilityName.text = hero.specialName ?? "";
            if (_abilityDesc != null)
                _abilityDesc.text = hero.specialDescription ?? "";

            // Compact stats (5 badges in a row)
            BuildCompactStats(hero);

            _btnSelect?.SetEnabled(true);

            // Notify listeners (e.g. HeroPreview3D)
            OnHeroChanged?.Invoke(hero);
        }

        private void BuildCompactStats(HeroConfig hero)
        {
            if (_statsContainer == null) return;
            _statsContainer.Clear();

            AddStatBadge("STP", hero.steps.ToString(), new Color(1f, 0.42f, 0.21f));
            AddStatBadge("RNG", hero.range.ToString(), new Color(1f, 0.42f, 0.21f));
            AddStatBadge("CD", hero.cooldown.ToString(), new Color(0.25f, 0.45f, 0.95f));
            AddStatBadge("ARM", hero.armor.ToString(), new Color(0.6f, 0.6f, 0.69f));
            AddStatBadge("SPD", hero.speed.ToString(), new Color(0.29f, 0.87f, 0.5f));
        }

        private void AddStatBadge(string label, string value, Color color)
        {
            var badge = new VisualElement();
            badge.style.flexGrow = 1;
            badge.style.alignItems = Align.Center;
            badge.style.justifyContent = Justify.Center;
            badge.style.backgroundColor = new Color(0.1f, 0.1f, 0.16f);
            badge.style.borderTopLeftRadius = 16;
            badge.style.borderTopRightRadius = 16;
            badge.style.borderBottomLeftRadius = 16;
            badge.style.borderBottomRightRadius = 16;
            badge.style.paddingTop = 10;
            badge.style.paddingBottom = 10;
            badge.style.marginRight = 8;

            var valLabel = new Label(value);
            valLabel.style.fontSize = 32;
            valLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valLabel.style.color = new StyleColor(color);
            valLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.Add(valLabel);

            var nameLabel = new Label(label);
            nameLabel.style.fontSize = 18;
            nameLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.69f));
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.marginTop = 2;
            badge.Add(nameLabel);

            _statsContainer.Add(badge);
        }

        #endregion

        #region UI Events

        private void HandleSelect(ClickEvent _)
        {
            if (_selectedHero == null) return;

            if (_isPassAndPlay && !_isP2Selecting)
            {
                _p1Hero = _selectedHero;
                _isP2Selecting = true;
                _selectedHero = null;
                UpdateTitle();

                if (_rosterGrid != null)
                    foreach (var child in _rosterGrid.Children())
                        child.RemoveFromClassList("hero-card--selected");

                if (_heroes != null)
                    foreach (var h in _heroes)
                        if (h.isUnlocked) { SelectHero(h); break; }
            }
            else
            {
                var p2 = _selectedHero;
                OnHeroesSelected?.Invoke(_isPassAndPlay ? _p1Hero : _selectedHero, p2);
            }
        }

        private void HandleBack(ClickEvent _)
        {
            if (_isP2Selecting)
            {
                _isP2Selecting = false;
                _selectedHero = null;
                UpdateTitle();

                if (_rosterGrid != null)
                    foreach (var child in _rosterGrid.Children())
                        child.RemoveFromClassList("hero-card--selected");

                if (_heroes != null)
                    foreach (var h in _heroes)
                        if (h.isUnlocked) { SelectHero(h); break; }
                return;
            }

            OnBackPressed?.Invoke();
        }

        private void UpdateTitle()
        {
            if (_subtitle != null)
            {
                if (!_isPassAndPlay)
                    _subtitle.text = "";
                else
                    _subtitle.text = _isP2Selecting ? "Player 2" : "Player 1";
            }
        }

        #endregion
    }
}
