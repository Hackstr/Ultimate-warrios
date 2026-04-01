using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    public class SettingsController : BackableScreenController
    {
        protected override ScreenTransition Transition => ScreenTransition.SlideLeft;
        private Button _btnMusic;
        private Button _btnSfx;
        private Button _btnVibration;
        private Label _musicValue;
        private Label _sfxValue;
        private Label _vibrationValue;

        private bool _musicOn;
        private bool _sfxOn;
        private bool _vibrationOn;

        protected override void QueryElements()
        {
            base.QueryElements();
            _btnMusic = Root.Q<Button>("btn-music");
            _btnSfx = Root.Q<Button>("btn-sfx");
            _btnVibration = Root.Q<Button>("btn-vibration");
            _musicValue = Root.Q<Label>("music-value");
            _sfxValue = Root.Q<Label>("sfx-value");
            _vibrationValue = Root.Q<Label>("vibration-value");
        }

        protected override void BindEvents()
        {
            base.BindEvents();
            _btnMusic?.RegisterCallback<ClickEvent>(HandleToggleMusic);
            _btnSfx?.RegisterCallback<ClickEvent>(HandleToggleSfx);
            _btnVibration?.RegisterCallback<ClickEvent>(HandleToggleVibration);
        }

        protected override void UnbindEvents()
        {
            base.UnbindEvents();
            _btnMusic?.UnregisterCallback<ClickEvent>(HandleToggleMusic);
            _btnSfx?.UnregisterCallback<ClickEvent>(HandleToggleSfx);
            _btnVibration?.UnregisterCallback<ClickEvent>(HandleToggleVibration);
        }

        protected override void OnShow()
        {
            // Load from PlayerPrefs
            _musicOn = PlayerPrefs.GetInt("music_on", 1) == 1;
            _sfxOn = PlayerPrefs.GetInt("sfx_on", 1) == 1;
            _vibrationOn = PlayerPrefs.GetInt("vibration_on", 0) == 1;
            UpdateToggles();
        }

        private void HandleToggleMusic(ClickEvent _)
        {
            _musicOn = !_musicOn;
            PlayerPrefs.SetInt("music_on", _musicOn ? 1 : 0);
            PlayerPrefs.Save();

            if (Gameplay.AudioManager.Instance != null)
                Gameplay.AudioManager.Instance.SetMusicVolume(_musicOn ? 0.5f : 0f);

            UpdateToggles();
        }

        private void HandleToggleSfx(ClickEvent _)
        {
            _sfxOn = !_sfxOn;
            PlayerPrefs.SetInt("sfx_on", _sfxOn ? 1 : 0);
            PlayerPrefs.Save();

            if (Gameplay.AudioManager.Instance != null)
                Gameplay.AudioManager.Instance.SetSFXVolume(_sfxOn ? 0.8f : 0f);

            UpdateToggles();
        }

        private void HandleToggleVibration(ClickEvent _)
        {
            _vibrationOn = !_vibrationOn;
            PlayerPrefs.SetInt("vibration_on", _vibrationOn ? 1 : 0);
            PlayerPrefs.Save();
            UpdateToggles();
        }

        private void UpdateToggles()
        {
            SetToggleLabel(_musicValue, _musicOn);
            SetToggleLabel(_sfxValue, _sfxOn);
            SetToggleLabel(_vibrationValue, _vibrationOn);
        }

        private static void SetToggleLabel(Label label, bool on)
        {
            if (label == null) return;
            label.text = on ? "ON" : "OFF";
            label.style.color = on
                ? new StyleColor(new Color(1f, 0.42f, 0.21f))
                : new StyleColor(new Color(0.6f, 0.6f, 0.69f));
        }
    }
}
