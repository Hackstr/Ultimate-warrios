using System;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Localization;

namespace TacticalDuelist.UI.Toolkit
{
    public class ProfileController : BackableScreenController
    {
        protected override ScreenTransition Transition => ScreenTransition.SlideLeft;
        private Label _avatarInitials;
        private Label _displayName;
        private Label _rankBadge;
        private Label _eloRating;
        private Label _statWins;
        private Label _statLosses;
        private Label _statMatches;
        private Label _statWinrate;
        private Label _favoriteHero;
        private Label _coinsValue;
        private Label _heroesUnlocked;
        private Label _memberSince;

        protected override void QueryElements()
        {
            base.QueryElements();
            _avatarInitials = Root.Q<Label>("avatar-initials");
            _displayName = Root.Q<Label>("display-name");
            _rankBadge = Root.Q<Label>("rank-badge");
            _eloRating = Root.Q<Label>("elo-rating");
            _statWins = Root.Q<Label>("stat-wins");
            _statLosses = Root.Q<Label>("stat-losses");
            _statMatches = Root.Q<Label>("stat-matches");
            _statWinrate = Root.Q<Label>("stat-winrate");
            _favoriteHero = Root.Q<Label>("favorite-hero");
            _coinsValue = Root.Q<Label>("coins-value");
            _heroesUnlocked = Root.Q<Label>("heroes-unlocked");
            _memberSince = Root.Q<Label>("member-since");
        }

        public void SetProfile(ProfileData data)
        {
            if (data == null) return;

            if (_displayName != null) _displayName.text = data.displayName ?? "Duelist";
            if (_avatarInitials != null)
            {
                string name = data.displayName ?? "DU";
                _avatarInitials.text = name.Length >= 2
                    ? name[..2].ToUpper()
                    : name.ToUpper();
            }

            if (_eloRating != null) _eloRating.text = data.rating.ToString();
            if (_rankBadge != null) _rankBadge.text = GetRankName(data.rating);

            if (_statWins != null) _statWins.text = data.wins.ToString();
            if (_statLosses != null) _statLosses.text = data.losses.ToString();

            int totalMatches = data.wins + data.losses + data.draws;
            if (_statMatches != null) _statMatches.text = totalMatches.ToString();

            float winrate = totalMatches > 0 ? (float)data.wins / totalMatches * 100f : 0f;
            if (_statWinrate != null)
            {
                _statWinrate.text = $"{winrate:F0}%";
                // Color-code: green > 55%, red < 45%, orange default
                _statWinrate.style.color = winrate >= 55f
                    ? new StyleColor(new Color(0.2f, 0.8f, 0.4f))
                    : winrate < 45f && totalMatches > 5
                        ? new StyleColor(new Color(0.85f, 0.2f, 0.2f))
                        : new StyleColor(new Color(1f, 0.42f, 0.21f));
            }

            if (_favoriteHero != null)
                _favoriteHero.text = string.IsNullOrEmpty(data.favoriteHero) ? "---" : CapitalizeFirst(data.favoriteHero);

            if (_coinsValue != null) _coinsValue.text = data.coins.ToString();
            if (_heroesUnlocked != null) _heroesUnlocked.text = $"{data.unlockedCount}/12";
            if (_memberSince != null) _memberSince.text = data.memberSince ?? "---";
        }

        /// <summary>Shows offline fallback data from local stats.</summary>
        public void SetOfflineProfile(string name, int wins, int losses)
        {
            var data = new ProfileData
            {
                displayName = name,
                rating = 1000,
                wins = wins,
                losses = losses,
                draws = 0,
                coins = 0,
                unlockedCount = 3,
                favoriteHero = null,
                memberSince = null
            };
            SetProfile(data);
        }

        private static string GetRankName(int elo)
        {
            if (elo >= 2000) return L.Get("rank_grandmaster");
            if (elo >= 1800) return L.Get("rank_master");
            if (elo >= 1600) return L.Get("rank_diamond");
            if (elo >= 1400) return L.Get("rank_platinum");
            if (elo >= 1200) return L.Get("rank_gold");
            if (elo >= 1000) return L.Get("rank_silver");
            return L.Get("rank_bronze");
        }

        private static string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s[1..];
        }
    }

    [Serializable]
    public class ProfileData
    {
        public string displayName;
        public int rating;
        public int wins;
        public int losses;
        public int draws;
        public int coins;
        public int unlockedCount;
        public string favoriteHero;
        public string memberSince;
    }

    /// <summary>
    /// Server response from /player/me — maps to ProfileData.
    /// </summary>
    [Serializable]
    public class ServerProfileResponse
    {
        public string id;
        public string displayName;
        public int rating;
        public int wins;
        public int losses;
        public int draws;
        public int coins;
        public string[] unlockedHeroes;
        public string favoriteHero;
        public string createdAt;

        public ProfileData ToProfileData()
        {
            return new ProfileData
            {
                displayName = displayName,
                rating = rating,
                wins = wins,
                losses = losses,
                draws = draws,
                coins = coins,
                unlockedCount = unlockedHeroes?.Length ?? 3,
                favoriteHero = favoriteHero,
                memberSince = ParseDate(createdAt)
            };
        }

        private static string ParseDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return null;
            if (DateTime.TryParse(iso, out var dt))
                return dt.ToString("MMM yyyy");
            return null;
        }
    }
}
