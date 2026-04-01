using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.UI.Toolkit
{
    public class RoundTransitionController : UIScreenBase
    {
        public event Action OnTransitionComplete;

        private Label _title;
        private Label _result;
        private Label _countdown;
        private VisualElement _shrinkWarning;
        private VisualElement _actionSummary;

        private float _elapsed;
        private const float DURATION = 2.5f;
        private bool _active;

        protected override void QueryElements()
        {
            _title = Root.Q<Label>("transition-title");
            _result = Root.Q<Label>("transition-result");
            _countdown = Root.Q<Label>("transition-countdown");
            _shrinkWarning = Root.Q("shrink-warning");
            _actionSummary = Root.Q("action-summary");
        }

        protected override void BindEvents() { }
        protected override void UnbindEvents() { }

        public void Show(int completedRound, RoundResult roundResult, bool mapShrinking,
                         List<ActionType> p1Actions = null, List<ActionType> p2Actions = null)
        {
            _elapsed = 0f;
            _active = true;

            if (_title != null)
            {
                int nextRound = completedRound + 1;
                string intensity = nextRound switch
                {
                    2 => "ROUND 2",
                    3 => "ROUND 3 — HEATING UP",
                    4 => "ROUND 4 — FINAL PUSH",
                    _ => nextRound >= 5 ? $"ROUND {nextRound} — SUDDEN DEATH" : $"ROUND {nextRound}"
                };
                _title.text = intensity;
            }

            if (_result != null)
            {
                _result.RemoveFromClassList("text-green");
                _result.RemoveFromClassList("text-red");
                _result.RemoveFromClassList("text-secondary");

                _result.text = roundResult switch
                {
                    RoundResult.Player1Kill => "Player 1 eliminated!",
                    RoundResult.Player2Kill => "Player 2 eliminated!",
                    RoundResult.MutualCancel => "Mutual cancel!",
                    _ => "No elimination"
                };

                _result.AddToClassList(roundResult switch
                {
                    RoundResult.Player1Kill => "text-green",
                    RoundResult.Player2Kill => "text-red",
                    _ => "text-secondary"
                });
            }

            if (_shrinkWarning != null)
                _shrinkWarning.style.display = mapShrinking
                    ? DisplayStyle.Flex : DisplayStyle.None;

            // Build action summary
            BuildActionSummary(p1Actions, p2Actions);

            if (_countdown != null)
                _countdown.text = "";

            base.Show();
        }

        private void BuildActionSummary(List<ActionType> p1, List<ActionType> p2)
        {
            if (_actionSummary == null) return;
            _actionSummary.Clear();

            if (p1 == null && p2 == null)
            {
                _actionSummary.style.display = DisplayStyle.None;
                return;
            }

            _actionSummary.style.display = DisplayStyle.Flex;

            // YOU row
            if (p1 != null)
            {
                var row = CreateActionRow("YOU", p1, new Color(1f, 0.42f, 0.21f));
                _actionSummary.Add(row);
            }

            // OPP row
            if (p2 != null)
            {
                var row = CreateActionRow("OPP", p2, new Color(0.25f, 0.45f, 0.95f));
                _actionSummary.Add(row);
            }
        }

        private VisualElement CreateActionRow(string label, List<ActionType> actions, Color color)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8;
            row.style.justifyContent = Justify.Center;

            var nameLabel = new Label(label);
            nameLabel.style.fontSize = 24;
            nameLabel.style.color = new StyleColor(color);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.width = 56;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            nameLabel.style.marginRight = 12;
            row.Add(nameLabel);

            foreach (var action in actions)
            {
                var badge = new VisualElement();
                badge.style.backgroundColor = new Color(0.1f, 0.1f, 0.16f);
                badge.style.borderTopLeftRadius = 8;
                badge.style.borderTopRightRadius = 8;
                badge.style.borderBottomLeftRadius = 8;
                badge.style.borderBottomRightRadius = 8;
                badge.style.paddingLeft = 8;
                badge.style.paddingRight = 8;
                badge.style.paddingTop = 4;
                badge.style.paddingBottom = 4;
                badge.style.marginRight = 4;

                var text = new Label(ActionShortName(action));
                text.style.fontSize = 20;
                text.style.color = new StyleColor(ActionColor(action));
                text.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.Add(text);

                row.Add(badge);
            }

            return row;
        }

        private static string ActionShortName(ActionType a) => a switch
        {
            ActionType.Move => "MOV",
            ActionType.TurnLeft => "TL",
            ActionType.TurnRight => "TR",
            ActionType.TurnAround => "UT",
            ActionType.Shoot => "SHT",
            ActionType.Wait => "WAT",
            ActionType.Special => "SPL",
            ActionType.Shield => "SHL",
            _ => "?"
        };

        private static Color ActionColor(ActionType a) => a switch
        {
            ActionType.Move => new Color(0.29f, 0.87f, 0.5f),
            ActionType.TurnLeft or ActionType.TurnRight or ActionType.TurnAround => new Color(0.38f, 0.65f, 0.98f),
            ActionType.Shoot => new Color(0.85f, 0.2f, 0.2f),
            ActionType.Wait => new Color(0.6f, 0.6f, 0.69f),
            ActionType.Special => new Color(1f, 0.85f, 0.2f),
            ActionType.Shield => new Color(0.22f, 0.74f, 0.97f),
            _ => new Color(0.6f, 0.6f, 0.69f)
        };

        public override void Tick(float dt)
        {
            if (!_active) return;

            _elapsed += dt;

            if (_countdown != null)
            {
                int remaining = Mathf.CeilToInt(DURATION - _elapsed);
                remaining = Mathf.Max(remaining, 0);
                _countdown.text = remaining > 0
                    ? $"Next round in {remaining}..."
                    : "GO!";
            }

            if (_elapsed >= DURATION)
            {
                _active = false;
                OnTransitionComplete?.Invoke();
            }
        }
    }
}
