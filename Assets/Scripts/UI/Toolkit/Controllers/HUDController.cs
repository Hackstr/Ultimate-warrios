using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.UI.Toolkit
{
    public class HUDController : UIScreenBase
    {
        public override void Show()
        {
            base.Show();
            // HUD is informational only — never block clicks to screens beneath
            SetPickingModeRecursive(Root, PickingMode.Ignore);
        }

        private static void SetPickingModeRecursive(VisualElement el, PickingMode mode)
        {
            if (el == null) return;
            el.pickingMode = mode;
            foreach (var child in el.Children())
                SetPickingModeRecursive(child, mode);
        }

        private Label _roundText;
        private Label _phaseText;
        private Label _stepText;
        private Label _timerText;
        private Label _p1Name;
        private Label _p2Name;
        private Label _p1Score;
        private Label _p2Score;
        private VisualElement _p1Frame;
        private VisualElement _p2Frame;
        private VisualElement _p1ArmorIcon;
        private VisualElement _p2ArmorIcon;
        private Label _combatText;

        private int _p1ScoreVal;
        private int _p2ScoreVal;
        private float _combatTextTimer;

        protected override void QueryElements()
        {
            _roundText = Root.Q<Label>("hud-round");
            _phaseText = Root.Q<Label>("hud-phase");
            _stepText = Root.Q<Label>("hud-step");
            _timerText = Root.Q<Label>("hud-timer");
            _p1Name = Root.Q<Label>("hud-p1-name");
            _p2Name = Root.Q<Label>("hud-p2-name");
            _p1Score = Root.Q<Label>("hud-p1-score");
            _p2Score = Root.Q<Label>("hud-p2-score");
            _p1Frame = Root.Q("hud-p1-frame");
            _p2Frame = Root.Q("hud-p2-frame");
            _p1ArmorIcon = Root.Q("hud-p1-armor");
            _p2ArmorIcon = Root.Q("hud-p2-armor");
            _combatText = Root.Q<Label>("combat-text");
        }

        protected override void BindEvents()
        {
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
            GameEvents.OnStepResolved += HandleStepResolved;
            GameEvents.OnArmorChanged += HandleArmorChanged;
            GameEvents.OnHeroEliminated += HandleHeroEliminated;
            GameEvents.OnPlanningTimerTick += HandleTimerTick;
            GameEvents.OnRoundEnded += HandleRoundEnded;
        }

        protected override void UnbindEvents()
        {
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnStepResolved -= HandleStepResolved;
            GameEvents.OnArmorChanged -= HandleArmorChanged;
            GameEvents.OnHeroEliminated -= HandleHeroEliminated;
            GameEvents.OnPlanningTimerTick -= HandleTimerTick;
            GameEvents.OnRoundEnded -= HandleRoundEnded;
        }

        public void Initialize(HeroConfig p1Hero, HeroConfig p2Hero)
        {
            _p1ScoreVal = 0;
            _p2ScoreVal = 0;

            if (_p1Name != null)
            {
                _p1Name.text = p1Hero.displayName;
                _p1Name.style.color = new StyleColor(p1Hero.heroColor);
            }
            if (_p2Name != null)
            {
                _p2Name.text = p2Hero.displayName;
                _p2Name.style.color = new StyleColor(p2Hero.heroColor);
            }

            // Color frames with hero colors
            if (_p1Frame != null)
                _p1Frame.style.borderTopColor = _p1Frame.style.borderRightColor =
                    _p1Frame.style.borderBottomColor = _p1Frame.style.borderLeftColor =
                        new StyleColor(p1Hero.heroColor);
            if (_p2Frame != null)
                _p2Frame.style.borderTopColor = _p2Frame.style.borderRightColor =
                    _p2Frame.style.borderBottomColor = _p2Frame.style.borderLeftColor =
                        new StyleColor(p2Hero.heroColor);

            // Set initials in frames
            var p1Label = _p1Frame?.Q<Label>();
            if (p1Label != null)
            {
                p1Label.text = p1Hero.displayName.Length >= 2 ? p1Hero.displayName[..2].ToUpper() : p1Hero.displayName.ToUpper();
                p1Label.style.color = new StyleColor(p1Hero.heroColor);
            }
            var p2Label = _p2Frame?.Q<Label>();
            if (p2Label != null)
            {
                p2Label.text = p2Hero.displayName.Length >= 2 ? p2Hero.displayName[..2].ToUpper() : p2Hero.displayName.ToUpper();
                p2Label.style.color = new StyleColor(p2Hero.heroColor);
            }

            SetArmorVisible(1, p1Hero.armor > 0);
            SetArmorVisible(2, p2Hero.armor > 0);
            SetPlayerAlive(1, true);
            SetPlayerAlive(2, true);
            UpdateScore();
        }

        private void HandleRoundStarted(int round)
        {
            if (_roundText != null) _roundText.text = $"Round {round}";
            _currentStep = 0;
            _totalSteps = 4; // default, updated per hero
            if (_stepText != null) _stepText.text = "";
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

            if (_timerText != null)
                _timerText.style.display = phase == GamePhase.Planning
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private int _currentStep;
        private int _totalSteps;

        private void HandleStepResolved(StepResult result)
        {
            _currentStep++;
            if (_stepText != null)
                _stepText.text = $"Step {_currentStep}/{_totalSteps}";

            // Combat feedback text
            if (result.MutualCancel)
                ShowCombatText("BLOCKED!", new Color(1f, 0.85f, 0.2f));
            else if (result.P1Eliminated)
                ShowCombatText("ELIMINATED!", new Color(0.85f, 0.2f, 0.2f));
            else if (result.P2Eliminated)
                ShowCombatText("ELIMINATED!", new Color(0.2f, 0.85f, 0.4f));
            else if (result.P1Hit || result.P2Hit)
                ShowCombatText("HIT!", new Color(1f, 0.42f, 0.21f));
            else if (result.P1ArmorBroken || result.P2ArmorBroken)
                ShowCombatText("ARMOR BREAK!", new Color(0.25f, 0.45f, 0.95f));
            else if (result.P1Fired || result.P2Fired)
                ShowCombatText("MISS", new Color(0.6f, 0.6f, 0.69f));
        }

        public void ShowCombatText(string text, Color color)
        {
            if (_combatText == null) return;
            _combatText.text = text;
            _combatText.style.color = new StyleColor(color);
            _combatText.style.opacity = 1f;
            _combatText.style.scale = new StyleScale(new Scale(new Vector3(1.3f, 1.3f, 1f)));
            _combatTextTimer = 1.2f;

            // Animate: scale down to 1
            _combatText.schedule.Execute(() =>
            {
                if (_combatText != null)
                    _combatText.style.scale = new StyleScale(new Scale(Vector3.one));
            }).ExecuteLater(50);
        }

        public override void Tick(float dt)
        {
            if (_combatTextTimer > 0f)
            {
                _combatTextTimer -= dt;
                if (_combatTextTimer <= 0.3f && _combatText != null)
                    _combatText.style.opacity = _combatTextTimer / 0.3f;
            }
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
            if (_timerText != null) _timerText.text = seconds.ToString();
        }

        private void HandleRoundEnded(RoundResult result)
        {
            if (result == RoundResult.Player1Kill) _p1ScoreVal++;
            else if (result == RoundResult.Player2Kill) _p2ScoreVal++;
            UpdateScore();
        }

        private void SetArmorVisible(int playerIndex, bool visible)
        {
            var icon = playerIndex == 1 ? _p1ArmorIcon : _p2ArmorIcon;
            if (icon != null)
                icon.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetPlayerAlive(int playerIndex, bool alive)
        {
            var frame = playerIndex == 1 ? _p1Frame : _p2Frame;
            if (frame == null) return;

            frame.RemoveFromClassList("frame--alive");
            frame.RemoveFromClassList("frame--eliminated");
            frame.AddToClassList(alive ? "frame--alive" : "frame--eliminated");
        }

        private void UpdateScore()
        {
            if (_p1Score != null) _p1Score.text = _p1ScoreVal.ToString();
            if (_p2Score != null) _p2Score.text = _p2ScoreVal.ToString();
        }
    }
}
