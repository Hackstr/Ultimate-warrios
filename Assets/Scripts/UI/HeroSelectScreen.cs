using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Systems;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Hero selection screen. Supports pass-and-play (MVP) and online modes.
    /// Shows hero roster, stats, and 3D preview. Fires HeroesSelected event when done.
    /// </summary>
    public class HeroSelectScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hero Data")]
        [SerializeField] private HeroConfig[] _availableHeroes;

        [Header("UI References — Header")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _backButton;

        [Header("UI References — Hero Cards")]
        [SerializeField] private Transform _heroCardContainer;
        [SerializeField] private GameObject _heroCardPrefab;

        [Header("UI References — Selected Hero Info")]
        [SerializeField] private TextMeshProUGUI _heroNameText;
        [SerializeField] private TextMeshProUGUI _difficultyText;
        [SerializeField] private TextMeshProUGUI _specialNameText;
        [SerializeField] private TextMeshProUGUI _specialDescText;

        [Header("UI References — Stat Bars")]
        [SerializeField] private Slider _stepsBar;
        [SerializeField] private Slider _rangeBar;
        [SerializeField] private Slider _cooldownBar;
        [SerializeField] private Slider _armorBar;
        [SerializeField] private Slider _speedBar;

        [Header("UI References — Stat Value Labels")]
        [SerializeField] private TextMeshProUGUI _stepsValueText;
        [SerializeField] private TextMeshProUGUI _rangeValueText;
        [SerializeField] private TextMeshProUGUI _cooldownValueText;
        [SerializeField] private TextMeshProUGUI _armorValueText;
        [SerializeField] private TextMeshProUGUI _speedValueText;

        [Header("UI References — 3D Preview")]
        [SerializeField] private Transform _previewSpawnPoint;

        [Header("UI References — Actions")]
        [SerializeField] private Button _selectButton;
        [SerializeField] private TextMeshProUGUI _selectButtonText;

        [Header("Pass-and-Play")]
        [SerializeField] private GameObject _passDeviceOverlay;
        [SerializeField] private Button _passDeviceTapButton;

        [Header("Stat Limits (for bar normalization)")]
        [SerializeField] private float _maxSteps = 6f;
        [SerializeField] private float _maxRange = 10f;
        [SerializeField] private float _maxCooldown = 3f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when both players have selected heroes (pass-and-play)
        /// or when the local player confirmed (online).
        /// </summary>
        public event Action<HeroConfig, HeroConfig> OnHeroesSelected;

        /// <summary>
        /// Fired when Back is pressed.
        /// </summary>
        public event Action OnBackPressed;

        #endregion

        #region Fields

        private int _currentPlayerSelecting = 1;
        private HeroConfig _selectedHero;
        private HeroConfig _player1Hero;
        private HeroConfig _player2Hero;
        private GameObject _previewInstance;
        private readonly List<HeroCardUI> _cards = new();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _selectButton?.onClick.AddListener(OnSelectPressed);
            _backButton?.onClick.AddListener(OnBackClicked);
            _passDeviceTapButton?.onClick.AddListener(OnPassDeviceTapped);
        }

        private void OnDisable()
        {
            _selectButton?.onClick.RemoveListener(OnSelectPressed);
            _backButton?.onClick.RemoveListener(OnBackClicked);
            _passDeviceTapButton?.onClick.RemoveListener(OnPassDeviceTapped);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the screen. Call when transitioning to hero select.
        /// </summary>
        public void Show(bool isPassAndPlay = true)
        {
            gameObject.SetActive(true);
            _currentPlayerSelecting = 1;
            _player1Hero = null;
            _player2Hero = null;

            HidePassDeviceOverlay();
            BuildHeroCards();
            UpdateTitle();

            if (_availableHeroes.Length > 0)
                SelectHero(_availableHeroes[0]);

            _selectButton.interactable = true;
        }

        public void Hide()
        {
            ClearPreview();
            gameObject.SetActive(false);
        }

        #endregion

        #region Private — Card Building

        private void BuildHeroCards()
        {
            foreach (var card in _cards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }
            _cards.Clear();

            if (_heroCardPrefab == null || _heroCardContainer == null) return;

            foreach (var hero in _availableHeroes)
            {
                var cardObj = Instantiate(_heroCardPrefab, _heroCardContainer);
                var cardUI = cardObj.GetComponent<HeroCardUI>();

                if (cardUI != null)
                {
                    cardUI.Setup(hero, OnHeroCardClicked);
                    _cards.Add(cardUI);
                }
            }
        }

        private void OnHeroCardClicked(HeroConfig hero)
        {
            SelectHero(hero);
        }

        #endregion

        #region Private — Hero Selection Display

        private void SelectHero(HeroConfig hero)
        {
            _selectedHero = hero;

            UpdateHeroInfo(hero);
            UpdateStatBars(hero);
            UpdatePreview(hero);
            HighlightCard(hero);
        }

        private void UpdateHeroInfo(HeroConfig hero)
        {
            if (_heroNameText != null)
                _heroNameText.text = hero.displayName;

            if (_difficultyText != null)
            {
                string stars = new string('★', hero.difficulty) + new string('☆', 5 - hero.difficulty);
                _difficultyText.text = stars;
            }

            if (_specialNameText != null)
                _specialNameText.text = hero.specialName;

            if (_specialDescText != null)
                _specialDescText.text = hero.specialDescription;
        }

        private void UpdateStatBars(HeroConfig hero)
        {
            SetBar(_stepsBar, _stepsValueText, hero.steps, _maxSteps);
            SetBar(_rangeBar, _rangeValueText, hero.range, _maxRange);
            SetBar(_cooldownBar, _cooldownValueText, hero.cooldown, _maxCooldown);
            SetBar(_armorBar, _armorValueText, hero.armor, 1f);
            SetBar(_speedBar, _speedValueText, hero.speed, 2f);
        }

        private void SetBar(Slider bar, TextMeshProUGUI label, float value, float max)
        {
            if (bar != null)
                bar.value = value / max;

            if (label != null)
                label.text = value.ToString("0");
        }

        private void UpdatePreview(HeroConfig hero)
        {
            ClearPreview();

            if (_previewSpawnPoint == null || hero.heroPrefab == null) return;

            _previewInstance = Instantiate(hero.heroPrefab, _previewSpawnPoint);
            _previewInstance.transform.localPosition = Vector3.zero;
            _previewInstance.transform.localRotation = Quaternion.identity;
        }

        private void ClearPreview()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
        }

        private void HighlightCard(HeroConfig hero)
        {
            foreach (var card in _cards)
                card?.SetHighlighted(card.Hero == hero);
        }

        #endregion

        #region Private — Button Handlers

        private void OnSelectPressed()
        {
            if (_selectedHero == null) return;

            if (_currentPlayerSelecting == 1)
            {
                _player1Hero = _selectedHero;
                _currentPlayerSelecting = 2;
                ShowPassDeviceOverlay();
            }
            else
            {
                _player2Hero = _selectedHero;
                _selectButton.interactable = false;
                OnHeroesSelected?.Invoke(_player1Hero, _player2Hero);
            }
        }

        private void OnBackClicked()
        {
            if (_currentPlayerSelecting == 2)
            {
                _currentPlayerSelecting = 1;
                _player1Hero = null;
                HidePassDeviceOverlay();
                UpdateTitle();
                return;
            }

            OnBackPressed?.Invoke();
        }

        private void OnPassDeviceTapped()
        {
            HidePassDeviceOverlay();
            UpdateTitle();

            if (_availableHeroes.Length > 0)
                SelectHero(_availableHeroes[0]);
        }

        #endregion

        #region Private — Pass Device

        private void ShowPassDeviceOverlay()
        {
            if (_passDeviceOverlay != null)
                _passDeviceOverlay.SetActive(true);
        }

        private void HidePassDeviceOverlay()
        {
            if (_passDeviceOverlay != null)
                _passDeviceOverlay.SetActive(false);
        }

        private void UpdateTitle()
        {
            if (_titleText != null)
                _titleText.text = _currentPlayerSelecting == 1
                    ? "PLAYER 1 — CHOOSE HERO"
                    : "PLAYER 2 — CHOOSE HERO";

            if (_selectButtonText != null)
                _selectButtonText.text = _currentPlayerSelecting == 1
                    ? "SELECT"
                    : "START MATCH";
        }

        #endregion
    }
}
