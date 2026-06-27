using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Localization;

namespace TacticalDuelist.UI.Toolkit
{
    public class LeaderboardController : BackableScreenController
    {
        protected override ScreenTransition Transition => ScreenTransition.SlideLeft;
        private VisualElement _listContainer;
        private Label _statusLabel;

        protected override void QueryElements()
        {
            base.QueryElements();
            _listContainer = Root.Q("leaderboard-list");
            _statusLabel = Root.Q<Label>("leaderboard-status");
        }

        public void SetData(List<LeaderboardEntry> entries)
        {
            if (_listContainer == null) return;
            _listContainer.Clear();

            if (entries == null || entries.Count == 0)
            {
                if (_statusLabel != null) _statusLabel.text = L.Get("no_players");
                return;
            }

            if (_statusLabel != null)
                _statusLabel.style.display = DisplayStyle.None;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 12;
                row.style.paddingBottom = 12;
                row.style.paddingLeft = 16;
                row.style.paddingRight = 16;
                row.style.borderTopLeftRadius = 16;
                row.style.borderTopRightRadius = 16;
                row.style.borderBottomLeftRadius = 16;
                row.style.borderBottomRightRadius = 16;
                row.style.marginBottom = 8;
                row.style.backgroundColor = i < 3
                    ? new Color(0.12f, 0.10f, 0.06f) // top 3 highlighted
                    : new Color(0.1f, 0.1f, 0.16f);

                // Rank number
                var rankLabel = new Label($"#{i + 1}");
                rankLabel.style.fontSize = 28;
                rankLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                rankLabel.style.width = 64;
                rankLabel.style.color = i == 0
                    ? new StyleColor(new Color(1f, 0.85f, 0.2f)) // gold
                    : i == 1
                        ? new StyleColor(new Color(0.75f, 0.75f, 0.8f)) // silver
                        : i == 2
                            ? new StyleColor(new Color(0.8f, 0.5f, 0.2f)) // bronze
                            : new StyleColor(new Color(0.6f, 0.6f, 0.69f));
                row.Add(rankLabel);

                // Rank tier dot
                var rankColor = ProfileController.GetRankColor(entry.rating);
                var rankDot = new VisualElement();
                rankDot.style.width = 16;
                rankDot.style.height = 16;
                rankDot.style.borderTopLeftRadius = 8;
                rankDot.style.borderTopRightRadius = 8;
                rankDot.style.borderBottomLeftRadius = 8;
                rankDot.style.borderBottomRightRadius = 8;
                rankDot.style.backgroundColor = rankColor;
                rankDot.style.marginRight = 8;
                row.Add(rankDot);

                // Name
                var nameLabel = new Label(entry.displayName ?? "Duelist");
                nameLabel.style.fontSize = 28;
                nameLabel.style.color = new StyleColor(new Color(0.92f, 0.92f, 0.96f));
                nameLabel.style.flexGrow = 1;
                row.Add(nameLabel);

                // W/L
                var wlLabel = new Label($"{entry.wins}W {entry.losses}L");
                wlLabel.style.fontSize = 22;
                wlLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.69f));
                wlLabel.style.marginRight = 16;
                row.Add(wlLabel);

                // Rating
                var ratingLabel = new Label(entry.rating.ToString());
                ratingLabel.style.fontSize = 30;
                ratingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                ratingLabel.style.color = new StyleColor(new Color(1f, 0.42f, 0.21f));
                ratingLabel.style.width = 80;
                ratingLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(ratingLabel);

                _listContainer.Add(row);
            }
        }

        public void SetLoading()
        {
            if (_statusLabel != null)
            {
                _statusLabel.style.display = DisplayStyle.Flex;
                _statusLabel.text = L.Get("loading");
            }
        }
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public string id;
        public string displayName;
        public int rating;
        public int wins;
        public int losses;
        public int rankTier;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] items;
    }
}
