using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Localization;

namespace TacticalDuelist.UI.Toolkit
{
    public class RevealController : UIScreenBase
    {
        public event Action OnRevealComplete;

        private Label _title;
        private Label _executeLabel;
        private VisualElement _p1Actions;
        private VisualElement _p2Actions;

        private float _elapsed;
        private int _revealedCount;
        private int _totalActions;
        private List<ActionType> _p1List;
        private List<ActionType> _p2List;
        private bool _active;
        private bool _executeSent;

        private const float TITLE_DURATION = 0.6f;
        private const float ACTION_DELAY = 0.25f;
        private const float POST_REVEAL_PAUSE = 1.0f;

        protected override void QueryElements()
        {
            _title = Root.Q<Label>("reveal-title");
            _executeLabel = Root.Q<Label>("reveal-execute");
            _p1Actions = Root.Q("reveal-p1-actions");
            _p2Actions = Root.Q("reveal-p2-actions");
        }

        protected override void BindEvents() { }
        protected override void UnbindEvents() { }

        public void Show(List<ActionType> p1Actions, List<ActionType> p2Actions)
        {
            _p1List = p1Actions ?? new List<ActionType>();
            _p2List = p2Actions ?? new List<ActionType>();
            _totalActions = Mathf.Max(_p1List.Count, _p2List.Count);
            _revealedCount = 0;
            _elapsed = 0f;
            _active = true;
            _executeSent = false;

            if (_p1Actions != null) _p1Actions.Clear();
            if (_p2Actions != null) _p2Actions.Clear();
            if (_executeLabel != null)
            {
                _executeLabel.text = "";
                _executeLabel.style.opacity = 0f;
            }

            base.Show();
        }

        public override void Tick(float dt)
        {
            if (!_active) return;
            _elapsed += dt;

            // Phase 1: Title visible (0 - TITLE_DURATION)
            if (_elapsed < TITLE_DURATION) return;

            // Phase 2: Reveal actions one by one
            float actionTime = _elapsed - TITLE_DURATION;
            int shouldReveal = Mathf.FloorToInt(actionTime / ACTION_DELAY);
            shouldReveal = Mathf.Min(shouldReveal, _totalActions);

            while (_revealedCount < shouldReveal)
            {
                RevealAction(_revealedCount);
                _revealedCount++;
            }

            // Phase 3: All revealed — show EXECUTE
            float totalRevealTime = TITLE_DURATION + _totalActions * ACTION_DELAY;
            if (_elapsed >= totalRevealTime && !_executeSent)
            {
                if (_executeLabel != null)
                {
                    _executeLabel.text = L.Get("execute");
                    _executeLabel.style.opacity = 1f;
                }
                _executeSent = true;
            }

            // Phase 4: After pause — trigger complete
            if (_elapsed >= totalRevealTime + POST_REVEAL_PAUSE)
            {
                _active = false;
                OnRevealComplete?.Invoke();
            }
        }

        private void RevealAction(int index)
        {
            if (index < _p1List.Count)
                AddActionBadge(_p1Actions, _p1List[index], new Color(1f, 0.42f, 0.21f));

            if (index < _p2List.Count)
                AddActionBadge(_p2Actions, _p2List[index], new Color(0.25f, 0.45f, 0.95f));
        }

        private void AddActionBadge(VisualElement container, ActionType action, Color color)
        {
            if (container == null) return;

            var badge = new VisualElement();
            badge.style.backgroundColor = new Color(0.1f, 0.1f, 0.16f);
            badge.style.borderTopLeftRadius = 12;
            badge.style.borderTopRightRadius = 12;
            badge.style.borderBottomLeftRadius = 12;
            badge.style.borderBottomRightRadius = 12;
            badge.style.paddingLeft = 16;
            badge.style.paddingRight = 16;
            badge.style.paddingTop = 8;
            badge.style.paddingBottom = 8;
            badge.style.marginBottom = 6;
            badge.style.width = Length.Percent(90);
            badge.style.alignItems = Align.Center;
            // Pop-in animation
            badge.style.opacity = 0f;
            badge.style.scale = new StyleScale(new Scale(new Vector3(0.8f, 0.8f, 1f)));
            badge.schedule.Execute(() =>
            {
                badge.style.opacity = 1f;
                badge.style.scale = new StyleScale(new Scale(Vector3.one));
            }).ExecuteLater(16);

            var text = new Label(ActionName(action));
            text.style.fontSize = 28;
            text.style.color = new StyleColor(color);
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.Add(text);

            container.Add(badge);
        }

        private static string ActionName(ActionType a) => a switch
        {
            ActionType.Move => L.Get("action_move"),
            ActionType.TurnLeft => L.Get("action_turn_left"),
            ActionType.TurnRight => L.Get("action_turn_right"),
            ActionType.TurnAround => L.Get("action_uturn"),
            ActionType.Shoot => L.Get("action_shoot"),
            ActionType.Wait => L.Get("action_wait"),
            ActionType.Special => L.Get("action_special_full"),
            ActionType.Shield => L.Get("action_shield_full"),
            _ => "?"
        };
    }
}
