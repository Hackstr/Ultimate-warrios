using System;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Localization;

namespace TacticalDuelist.UI.Toolkit
{
    public class DailyRewardController : UIScreenBase
    {
        public event Action OnClaimed;
        public event Action OnDismissed;

        private static readonly int[] Rewards = { 25, 50, 75, 100, 150, 200, 300 };

        private VisualElement _daysContainer;
        private Label _streakLabel;
        private Label _rewardLabel;
        private Label _heroUnlockLabel;
        private Button _btnClaim;
        private Button _btnClose;

        private int _streak;
        private bool _canClaim;
        private int _nextReward;

        protected override void QueryElements()
        {
            _daysContainer = Root.Q("days-container");
            _streakLabel = Root.Q<Label>("streak-label");
            _rewardLabel = Root.Q<Label>("reward-label");
            _heroUnlockLabel = Root.Q<Label>("hero-unlock-label");
            _btnClaim = Root.Q<Button>("btn-claim");
            _btnClose = Root.Q<Button>("btn-close");
        }

        protected override void BindEvents()
        {
            _btnClaim?.RegisterCallback<ClickEvent>(HandleClaim);
            _btnClose?.RegisterCallback<ClickEvent>(HandleClose);
        }

        protected override void UnbindEvents()
        {
            _btnClaim?.UnregisterCallback<ClickEvent>(HandleClaim);
            _btnClose?.UnregisterCallback<ClickEvent>(HandleClose);
        }

        public void SetData(int currentStreak, bool canClaim, int nextReward)
        {
            _streak = currentStreak;
            _canClaim = canClaim;
            _nextReward = nextReward;

            int nextDay = canClaim ? (currentStreak % 7) + 1 : currentStreak;
            if (_streakLabel != null)
                _streakLabel.text = L.Get("day_of", nextDay);

            if (_rewardLabel != null)
                _rewardLabel.text = L.Get("plus_coins", nextReward);

            if (_heroUnlockLabel != null)
            {
                _heroUnlockLabel.style.display = nextDay == 7
                    ? DisplayStyle.Flex : DisplayStyle.None;
                _heroUnlockLabel.text = L.Get("day7_bonus");
            }

            if (_btnClaim != null)
            {
                _btnClaim.text = canClaim ? L.Get("claim") : L.Get("claimed");
                _btnClaim.SetEnabled(canClaim);
            }

            BuildDayStrip(nextDay);
            base.Show();
        }

        public void ShowClaimResult(int coinsAwarded, string unlockedHero)
        {
            _canClaim = false;

            if (_rewardLabel != null)
                _rewardLabel.text = L.Get("plus_coins", coinsAwarded);

            if (_btnClaim != null)
            {
                _btnClaim.text = L.Get("claimed");
                _btnClaim.SetEnabled(false);
            }

            if (!string.IsNullOrEmpty(unlockedHero) && _heroUnlockLabel != null)
            {
                _heroUnlockLabel.style.display = DisplayStyle.Flex;
                _heroUnlockLabel.text = L.Get("hero_unlocked", unlockedHero.ToUpper());
            }
        }

        private void BuildDayStrip(int activeDay)
        {
            if (_daysContainer == null) return;
            _daysContainer.Clear();

            for (int i = 0; i < 7; i++)
            {
                int day = i + 1;
                var slot = new VisualElement();
                slot.style.width = 110;
                slot.style.height = 120;
                slot.style.marginLeft = 6;
                slot.style.marginRight = 6;
                slot.style.borderTopLeftRadius = 16;
                slot.style.borderTopRightRadius = 16;
                slot.style.borderBottomLeftRadius = 16;
                slot.style.borderBottomRightRadius = 16;
                slot.style.alignItems = Align.Center;
                slot.style.justifyContent = Justify.Center;

                bool isPast = day < activeDay;
                bool isCurrent = day == activeDay;

                if (isCurrent)
                {
                    slot.style.backgroundColor = new Color(1f, 0.42f, 0.21f, 0.9f); // orange
                    slot.style.borderBottomWidth = 4;
                    slot.style.borderBottomColor = new Color(1f, 0.85f, 0.2f);
                }
                else if (isPast)
                {
                    slot.style.backgroundColor = new Color(0.2f, 0.6f, 0.3f, 0.8f); // green
                }
                else
                {
                    slot.style.backgroundColor = new Color(0.15f, 0.15f, 0.25f, 0.8f); // dark
                }

                var dayLabel = new Label($"D{day}");
                dayLabel.style.fontSize = 22;
                dayLabel.style.color = Color.white;
                dayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                slot.Add(dayLabel);

                var coinLabel = new Label(isPast ? "✓" : $"{Rewards[i]}");
                coinLabel.style.fontSize = isPast ? 32 : 24;
                coinLabel.style.color = isPast
                    ? new Color(0.7f, 1f, 0.7f)
                    : new Color(1f, 0.85f, 0.2f);
                slot.Add(coinLabel);

                _daysContainer.Add(slot);
            }
        }

        private void HandleClaim(ClickEvent _)
        {
            if (!_canClaim) return;
            OnClaimed?.Invoke();
        }

        private void HandleClose(ClickEvent _)
        {
            OnDismissed?.Invoke();
        }
    }
}
