using System;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.UI.Toolkit
{
    public class ResultController : UIScreenBase
    {
        protected override ScreenTransition Transition => ScreenTransition.ScaleFade;
        public event Action OnRematchRequested;
        public event Action OnMainMenuRequested;

        private Label _resultTitle;
        private Label _resultSubtitle;
        private Label _p1Name;
        private Label _p2Name;
        private Label _scoreLabel;
        private VisualElement _p1Frame;
        private VisualElement _p2Frame;
        private VisualElement _roundDetails;
        private VisualElement _rewardsContainer;
        private Button _btnRematch;
        private Button _btnMenu;
        private Button _btnGG;
        private Button _btnWP;

        protected override void QueryElements()
        {
            _resultTitle = Root.Q<Label>("result-title");
            _resultSubtitle = Root.Q<Label>("result-subtitle");
            _p1Name = Root.Q<Label>("p1-name");
            _p2Name = Root.Q<Label>("p2-name");
            _scoreLabel = Root.Q<Label>("score-label");
            _p1Frame = Root.Q("p1-frame");
            _p2Frame = Root.Q("p2-frame");
            _roundDetails = Root.Q("round-details");
            _rewardsContainer = Root.Q("rewards-container");
            _btnRematch = Root.Q<Button>("btn-rematch");
            _btnMenu = Root.Q<Button>("btn-menu");
            _btnGG = Root.Q<Button>("btn-gg");
            _btnWP = Root.Q<Button>("btn-wp");
        }

        protected override void BindEvents()
        {
            _btnRematch?.RegisterCallback<ClickEvent>(HandleRematch);
            _btnMenu?.RegisterCallback<ClickEvent>(HandleMenu);
            _btnGG?.RegisterCallback<ClickEvent>(HandleGG);
            _btnWP?.RegisterCallback<ClickEvent>(HandleWP);
        }

        protected override void UnbindEvents()
        {
            _btnRematch?.UnregisterCallback<ClickEvent>(HandleRematch);
            _btnMenu?.UnregisterCallback<ClickEvent>(HandleMenu);
            _btnGG?.UnregisterCallback<ClickEvent>(HandleGG);
            _btnWP?.UnregisterCallback<ClickEvent>(HandleWP);
        }

        public void Show(MatchResult result, HeroConfig p1Hero, HeroConfig p2Hero,
                         int p1Wins, int p2Wins, RoundResult[] roundResults = null)
        {
            UpdateResultTitle(result, p1Hero, p2Hero);
            UpdateHeroDisplay(p1Hero, p2Hero);
            UpdateWinnerFrames(result);
            UpdateScore(p1Wins, p2Wins);
            BuildRoundDetails(roundResults);
            base.Show();

            // Title bounce animation
            if (_resultTitle != null)
            {
                _resultTitle.style.scale = new StyleScale(new Scale(new UnityEngine.Vector3(0.5f, 0.5f, 1f)));
                _resultTitle.schedule.Execute(() =>
                {
                    if (_resultTitle != null)
                        _resultTitle.style.scale = new StyleScale(new Scale(UnityEngine.Vector3.one));
                }).ExecuteLater(50);
            }
        }

        private void UpdateResultTitle(MatchResult result, HeroConfig p1Hero, HeroConfig p2Hero)
        {
            if (_resultTitle == null) return;

            // Clear previous color classes
            _resultTitle.RemoveFromClassList("text-green");
            _resultTitle.RemoveFromClassList("text-red");
            _resultTitle.RemoveFromClassList("text-gold");

            switch (result)
            {
                case MatchResult.Player1Win:
                    _resultTitle.text = "VICTORY";
                    _resultTitle.AddToClassList("text-green");
                    if (_resultSubtitle != null)
                        _resultSubtitle.text = p1Hero != null ? p1Hero.displayName + " wins!" : "";
                    break;
                case MatchResult.Player2Win:
                    _resultTitle.text = "DEFEAT";
                    _resultTitle.AddToClassList("text-red");
                    if (_resultSubtitle != null)
                        _resultSubtitle.text = p2Hero != null ? p2Hero.displayName + " wins!" : "";
                    break;
                case MatchResult.Draw:
                    _resultTitle.text = "DRAW";
                    _resultTitle.AddToClassList("text-gold");
                    if (_resultSubtitle != null)
                        _resultSubtitle.text = "Neither duelist prevailed";
                    break;
            }
        }

        private void UpdateHeroDisplay(HeroConfig p1Hero, HeroConfig p2Hero)
        {
            if (_p1Name != null) _p1Name.text = p1Hero != null ? p1Hero.displayName : "P1";
            if (_p2Name != null) _p2Name.text = p2Hero != null ? p2Hero.displayName : "P2";
        }

        private void UpdateWinnerFrames(MatchResult result)
        {
            _p1Frame?.RemoveFromClassList("frame--winner");
            _p2Frame?.RemoveFromClassList("frame--winner");

            if (result == MatchResult.Player1Win)
                _p1Frame?.AddToClassList("frame--winner");
            else if (result == MatchResult.Player2Win)
                _p2Frame?.AddToClassList("frame--winner");
        }

        private void UpdateScore(int p1Wins, int p2Wins)
        {
            if (_scoreLabel != null)
                _scoreLabel.text = $"{p1Wins} — {p2Wins}";
        }

        private void BuildRoundDetails(RoundResult[] roundResults)
        {
            if (_roundDetails == null) return;
            _roundDetails.Clear();

            if (roundResults == null) return;

            for (int i = 0; i < roundResults.Length; i++)
            {
                var label = new Label(FormatRoundResult(i + 1, roundResults[i]));
                label.AddToClassList("result__round-line");

                // Color-code round results
                label.AddToClassList(roundResults[i] switch
                {
                    RoundResult.Player1Kill => "text-green",
                    RoundResult.Player2Kill => "text-red",
                    RoundResult.MutualCancel => "text-gold",
                    _ => "text-secondary"
                });

                _roundDetails.Add(label);
            }
        }

        private static string FormatRoundResult(int round, RoundResult result) => result switch
        {
            RoundResult.Player1Kill => $"Round {round}: P1 Kill",
            RoundResult.Player2Kill => $"Round {round}: P2 Kill",
            RoundResult.MutualCancel => $"Round {round}: Mutual Cancel",
            RoundResult.NoKill => $"Round {round}: No Kill",
            _ => $"Round {round}: ---"
        };

        /// <summary>
        /// Show rating change and coins earned after online match.
        /// </summary>
        public void ShowRewards(int ratingDelta, int coinsEarned)
        {
            if (_rewardsContainer == null)
            {
                // Create rewards container dynamically if not in UXML
                _rewardsContainer = new VisualElement();
                _rewardsContainer.name = "rewards-container";
                _rewardsContainer.style.alignItems = Align.Center;
                _rewardsContainer.style.flexDirection = FlexDirection.Row;
                _rewardsContainer.style.justifyContent = Justify.Center;
                _rewardsContainer.style.marginTop = 22;

                // Insert before buttons
                var parent = _btnRematch?.parent ?? _btnMenu?.parent ?? Root;
                parent.Add(_rewardsContainer);
            }

            _rewardsContainer.Clear();
            _rewardsContainer.style.display = DisplayStyle.Flex;

            // ELO delta
            var eloLabel = new Label(ratingDelta >= 0 ? $"+{ratingDelta} ELO" : $"{ratingDelta} ELO");
            eloLabel.style.fontSize = 39;
            eloLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            eloLabel.style.color = ratingDelta >= 0
                ? new StyleColor(new UnityEngine.Color(0.2f, 0.85f, 0.4f))
                : new StyleColor(new UnityEngine.Color(0.85f, 0.2f, 0.2f));
            _rewardsContainer.Add(eloLabel);

            // Coins
            if (coinsEarned > 0)
            {
                var coinsLabel = new Label($"+{coinsEarned} coins");
                coinsLabel.style.marginLeft = 44;
                coinsLabel.style.fontSize = 39;
                coinsLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
                coinsLabel.style.color = new StyleColor(new UnityEngine.Color(1f, 0.84f, 0f));
                _rewardsContainer.Add(coinsLabel);
            }
        }

        public void HideRewards()
        {
            if (_rewardsContainer != null)
                _rewardsContainer.style.display = DisplayStyle.None;
        }

        public event Action<string> OnEmoteSent;

        private void HandleRematch(ClickEvent _) => OnRematchRequested?.Invoke();
        private void HandleMenu(ClickEvent _) => OnMainMenuRequested?.Invoke();

        private void HandleGG(ClickEvent _)
        {
            OnEmoteSent?.Invoke("GG");
            if (_btnGG != null) { _btnGG.SetEnabled(false); _btnGG.style.opacity = 0.4f; }
        }

        private void HandleWP(ClickEvent _)
        {
            OnEmoteSent?.Invoke("WP");
            if (_btnWP != null) { _btnWP.SetEnabled(false); _btnWP.style.opacity = 0.4f; }
        }
    }
}
