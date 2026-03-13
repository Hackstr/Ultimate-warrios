using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Main menu screen. Entry point for the game.
    /// Shows Play (offline) and future online/settings buttons.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _playOfflineButton;
        [SerializeField] private Button _playOnlineButton;

        #endregion

        #region Events

        public event Action OnPlayOffline;
        public event Action OnPlayOnline;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _playOfflineButton?.onClick.AddListener(HandlePlayOffline);
            _playOnlineButton?.onClick.AddListener(HandlePlayOnline);
        }

        private void OnDisable()
        {
            _playOfflineButton?.onClick.RemoveListener(HandlePlayOffline);
            _playOnlineButton?.onClick.RemoveListener(HandlePlayOnline);
        }

        #endregion

        #region Public Methods

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private

        private void HandlePlayOffline() => OnPlayOffline?.Invoke();
        private void HandlePlayOnline() => OnPlayOnline?.Invoke();

        #endregion
    }
}
