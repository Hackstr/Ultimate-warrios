using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Displays match results: winner/draw, hero portraits, round score, and rematch options.
    /// Subscribes to GameEvents.OnMatchEnded.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Result Display")]
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultSubtitleText;

        [Header("Hero Display")]
        [SerializeField] private Image _p1Portrait;
        [SerializeField] private Image _p2Portrait;
        [SerializeField] private TextMeshProUGUI _p1NameText;
        [SerializeField] private TextMeshProUGUI _p2NameText;

        [Header("Winner Highlight")]
        [SerializeField] private GameObject _p1WinnerFrame;
        [SerializeField] private GameObject _p2WinnerFrame;

        [Header("Score")]
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Round Details")]
        [SerializeField] private Transform _roundDetailContainer;
        [SerializeField] private GameObject _roundDetailPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Colors")]
        [SerializeField] private Color _winColor = new(0.2f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color _loseColor = new(0.6f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _drawColor = new(0.8f, 0.75f, 0.2f, 1f);

        #endregion

        #region Events

        public event Action OnRematchRequested;
        public event Action OnMainMenuRequested;

        #endregion

        #region Fields

        private HeroConfig _p1Hero;
        private HeroConfig _p2Hero;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _rematchButton?.onClick.AddListener(HandleRematch);
            _mainMenuButton?.onClick.AddListener(HandleMainMenu);
        }

        private void OnDisable()
        {
            _rematchButton?.onClick.RemoveListener(HandleRematch);
            _mainMenuButton?.onClick.RemoveListener(HandleMainMenu);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the result screen with match outcome data.
        /// </summary>
        public void Show(MatchResult result, HeroConfig p1Hero, HeroConfig p2Hero,
                         int p1Wins, int p2Wins, RoundResult[] roundResults = null)
        {
            gameObject.SetActive(true);
            _p1Hero = p1Hero;
            _p2Hero = p2Hero;

            UpdateResultTitle(result);
            UpdateHeroDisplay();
            UpdateWinnerFrames(result);
            UpdateScore(p1Wins, p2Wins);
            BuildRoundDetails(roundResults);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private — Display Updates

        private void UpdateResultTitle(MatchResult result)
        {
            if (_resultTitleText == null) return;

            switch (result)
            {
                case MatchResult.Player1Win:
                    _resultTitleText.text = "PLAYER 1 WINS";
                    _resultTitleText.color = _winColor;
                    if (_resultSubtitleText != null)
                        _resultSubtitleText.text = _p1Hero != null ? _p1Hero.displayName : "";
                    break;

                case MatchResult.Player2Win:
                    _resultTitleText.text = "PLAYER 2 WINS";
                    _resultTitleText.color = _winColor;
                    if (_resultSubtitleText != null)
                        _resultSubtitleText.text = _p2Hero != null ? _p2Hero.displayName : "";
                    break;

                case MatchResult.Draw:
                    _resultTitleText.text = "DRAW";
                    _resultTitleText.color = _drawColor;
                    if (_resultSubtitleText != null)
                        _resultSubtitleText.text = "Neither duelist prevailed";
                    break;
            }
        }

        private void UpdateHeroDisplay()
        {
            if (_p1Portrait != null && _p1Hero?.portrait != null)
                _p1Portrait.sprite = _p1Hero.portrait;

            if (_p2Portrait != null && _p2Hero?.portrait != null)
                _p2Portrait.sprite = _p2Hero.portrait;

            if (_p1NameText != null)
                _p1NameText.text = _p1Hero != null ? _p1Hero.displayName : "P1";

            if (_p2NameText != null)
                _p2NameText.text = _p2Hero != null ? _p2Hero.displayName : "P2";
        }

        private void UpdateWinnerFrames(MatchResult result)
        {
            if (_p1WinnerFrame != null)
                _p1WinnerFrame.SetActive(result == MatchResult.Player1Win);

            if (_p2WinnerFrame != null)
                _p2WinnerFrame.SetActive(result == MatchResult.Player2Win);
        }

        private void UpdateScore(int p1Wins, int p2Wins)
        {
            if (_scoreText != null)
                _scoreText.text = $"{p1Wins} — {p2Wins}";
        }

        private void BuildRoundDetails(RoundResult[] roundResults)
        {
            if (_roundDetailContainer == null || _roundDetailPrefab == null) return;

            foreach (Transform child in _roundDetailContainer)
                Destroy(child.gameObject);

            if (roundResults == null) return;

            for (int i = 0; i < roundResults.Length; i++)
            {
                var detailObj = Instantiate(_roundDetailPrefab, _roundDetailContainer);
                detailObj.SetActive(true);
                var label = detailObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = FormatRoundResult(i + 1, roundResults[i]);
            }
        }

        private static string FormatRoundResult(int roundNumber, RoundResult result) => result switch
        {
            RoundResult.Player1Kill => $"Round {roundNumber}: P1 Kill",
            RoundResult.Player2Kill => $"Round {roundNumber}: P2 Kill",
            RoundResult.MutualCancel => $"Round {roundNumber}: Mutual Cancel",
            RoundResult.NoKill => $"Round {roundNumber}: No Kill",
            _ => $"Round {roundNumber}: —"
        };

        #endregion

        #region Private — Button Handlers

        private void HandleRematch()
        {
            OnRematchRequested?.Invoke();
        }

        private void HandleMainMenu()
        {
            OnMainMenuRequested?.Invoke();
        }

        #endregion
    }
}
