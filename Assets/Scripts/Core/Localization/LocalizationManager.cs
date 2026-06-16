using System.Collections.Generic;
using UnityEngine;

namespace TacticalDuelist.Core.Localization
{
    /// <summary>
    /// Simple localization system. Loads key-value translations from JSON files
    /// in Resources/Localization/. Supports EN, RU, KZ.
    /// Usage: L.Get("key") or L.Get("key", param1, param2)
    /// </summary>
    public static class L
    {
        public static string CurrentLanguage { get; private set; } = "en";

        private static Dictionary<string, string> _strings = new();
        private static Dictionary<string, string> _fallback = new(); // English fallback
        private static bool _initialized;

        public static event System.Action OnLanguageChanged;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            // Load English as fallback
            _fallback = LoadLanguage("en");

            // Load saved language or detect from system
            var saved = PlayerPrefs.GetString("language", "");
            if (string.IsNullOrEmpty(saved))
                saved = DetectLanguage();

            SetLanguage(saved, notify: false);
        }

        public static void SetLanguage(string lang, bool notify = true)
        {
            CurrentLanguage = lang;
            _strings = lang == "en" ? _fallback : LoadLanguage(lang);
            PlayerPrefs.SetString("language", lang);
            PlayerPrefs.Save();

            if (notify)
                OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Get localized string by key. Returns key itself if not found.
        /// </summary>
        public static string Get(string key)
        {
            if (!_initialized) Init();

            if (_strings.TryGetValue(key, out var value))
                return value;
            if (_fallback.TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }

        /// <summary>
        /// Get localized string with format parameters.
        /// Example: L.Get("round_label", round) → "ROUND 2 — PLANNING"
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            var template = Get(key);
            try { return string.Format(template, args); }
            catch { return template; }
        }

        public static string[] GetAvailableLanguages() => new[] { "en", "ru", "kz" };

        public static string GetLanguageName(string code) => code switch
        {
            "en" => "English",
            "ru" => "Русский",
            "kz" => "Қазақша",
            _ => code
        };

        private static string DetectLanguage()
        {
            var sysLang = Application.systemLanguage;
            return sysLang switch
            {
                SystemLanguage.Russian => "ru",
                // Unity doesn't have Kazakh, check via OS
                _ => "en"
            };
        }

        private static Dictionary<string, string> LoadLanguage(string lang)
        {
            var dict = new Dictionary<string, string>();
            var asset = Resources.Load<TextAsset>($"Localization/{lang}");
            if (asset == null)
            {
                Debug.LogWarning($"[L] Localization file not found: Resources/Localization/{lang}.json");
                return dict;
            }

            try
            {
                var data = JsonUtility.FromJson<LocalizationData>(asset.text);
                if (data?.entries != null)
                {
                    foreach (var entry in data.entries)
                        dict[entry.key] = entry.value;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[L] Failed to parse {lang}.json: {ex.Message}");
            }

            return dict;
        }
    }

    [System.Serializable]
    public class LocalizationData
    {
        public LocalizationEntry[] entries;
    }

    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }
}
