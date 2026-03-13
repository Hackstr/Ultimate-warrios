using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// In-game HUD overlay. Shows round number, step counter, hero status,
    /// and current phase indicator. Subscribes to GameEvents for automatic updates.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Round & Phase")]
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private TextMeshProUGUI _phaseText;
        [SerializeField] private TextMeshProUGUI _stepText;

        [Header("Player 1")]
        [SerializeField] private TextMeshProUGUI _p1NameText;
        [SerializeField] private Image _p1Portrait;
        [SerializeField] private GameObject _p1ArmorIcon;
        [SerializeField] private Image _p1StatusFrame;

        [Header("Player 2")]
        [SerializeField] private TextMeshProUGUI _p2NameText;
        [SerializeField] private Image _p2Portrait;
        [SerializeField] private GameObject _p2ArmorIcon;
        [SerializeField] private Image _p2StatusFrame;

        [Header("Timer (shared with Planning)")]
        [SerializeField] private TextMeshProUGUI _hudTimerText;

        [Header("Score")]
        [SerializeField] private TextMeshProUGUI _p1ScoreText;
        [SerializeField] private TextMeshProUGUI _p2ScoreText;

        [Header("Colors")]
        [SerializeField] private Color _aliveColor = new(0.2f, 0.6f, 0.3f, 1f);
        [SerializeField] private Color _eliminatedColor = new(0.5f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color _neutralColor = new(0.3f, 0.3f, 0.4f, 1f);

        #endregion

        #region Fields

        private int _currentRound;
        private int _currentStep;
        private int _p1Score;
        private int _p2Score;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            GameEvents.OnMatchStarted += HandleMatchStarted;
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
            GameEvents.OnStepResolved += HandleStepResolved;
            GameEvents.OnArmorChanged += HandleArmorChanged;
            GameEvents.OnHeroEliminated += HandleHeroEliminated;
            GameEvents.OnPlanningTimerTick += HandleTimerTick;
            GameEvents.OnRoundEnded += HandleRoundEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchStarted -= HandleMatchStarted;
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnStepResolved -= HandleStepResolved;
            GameEvents.OnArmorChanged -= HandleArmorChanged;
            GameEvents.OnHeroEliminated -= HandleHeroEliminated;
            GameEvents.OnPlanningTimerTick -= HandleTimerTick;
            GameEvents.OnRoundEnded -= HandleRoundEnded;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes HUD with hero data. Called once at match start.
        /// </summary>
        public void Initialize(HeroConfig p1Hero, HeroConfig p2Hero)
        {
            _p1Score = 0;
            _p2Score = 0;

            if (_p1NameText != null)
                _p1NameText.text = p1Hero.displayName;
            if (_p2NameText != null)
                _p2NameText.text = p2Hero.displayName;

            if (_p1Portrait != null && p1Hero.portrait != null)
                _p1Portrait.sprite = p1Hero.portrait;
            if (_p2Portrait != null && p2Hero.portrait != null)
                _p2Portrait.sprite = p2Hero.portrait;

            SetArmorVisible(1, p1Hero.armor > 0);
            SetArmorVisible(2, p2Hero.armor > 0);

            SetPlayerAlive(1, true);
            SetPlayerAlive(2, true);

            UpdateScore();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        #endregion

        #region Private — Event Handlers

        private void HandleMatchStarted(MatchStartData data)
        {
            _currentRound = 0;
            _currentStep = 0;
            Initialize(data.P1Hero, data.P2Hero);
        }

        private void HandleRoundStarted(int round)
        {
            _currentRound = round;
            _currentStep = 0;

            if (_roundText != null)
                _roundText.text = $"Round {round}";

            if (_stepText != null)
                _stepText.text = "";

            SetPlayerAlive(1, true);
            SetPlayerAlive(2, true);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (_phaseText != null)
                _phaseText.text = phase switch
                {
                    GamePhase.Planning => "PLANNING",
                    GamePhase.Execution => "EXECUTING",
                    GamePhase.PostRound => "ROUND END",
                    GamePhase.PostMatch => "MATCH END",
                    _ => ""
                };

            if (_hudTimerText != null)
                _hudTimerText.gameObject.SetActive(phase == GamePhase.Planning);
        }

        private void HandleStepResolved(StepResult result)
        {
            _currentStep++;
            if (_stepText != null)
                _stepText.text = $"Step {_currentStep}";
        }

        private void HandleArmorChanged(int playerIndex, bool hasArmor)
        {
            SetArmorVisible(playerIndex, hasArmor);
        }

        private void HandleHeroEliminated(int playerIndex)
        {
            SetPlayerAlive(playerIndex, false);
        }

        private void HandleTimerTick(int seconds)
        {
            if (_hudTimerText != null)
                _hudTimerText.text = seconds.ToString();
        }

        private void HandleRoundEnded(RoundResult result)
        {
            switch (result)
            {
                case RoundResult.Player1Kill:
                    _p1Score++;
                    break;
                case RoundResult.Player2Kill:
                    _p2Score++;
                    break;
            }
            UpdateScore();
        }

        #endregion

        #region Private — Helpers

        private void SetArmorVisible(int playerIndex, bool visible)
        {
            var icon = playerIndex == 1 ? _p1ArmorIcon : _p2ArmorIcon;
            if (icon != null) icon.SetActive(visible);
        }

        private void SetPlayerAlive(int playerIndex, bool alive)
        {
            var frame = playerIndex == 1 ? _p1StatusFrame : _p2StatusFrame;
            if (frame != null) frame.color = alive ? _aliveColor : _eliminatedColor;
        }

        private void UpdateScore()
        {
            if (_p1ScoreText != null) _p1ScoreText.text = _p1Score.ToString();
            if (_p2ScoreText != null) _p2ScoreText.text = _p2Score.ToString();
        }

        #endregion
    }
}
