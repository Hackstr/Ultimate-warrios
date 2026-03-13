using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Matchmaking screen. Shows spinner animation and cancel button
    /// while searching for an opponent.
    /// </summary>
    public class MatchmakingScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Button _cancelButton;

        #endregion

        #region Events

        public event Action OnCancelMatchmaking;

        #endregion

        #region Fields

        private float _searchTime;
        private int _dotCount;
        private float _dotTimer;
        private const float DotInterval = 0.5f;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _cancelButton?.onClick.AddListener(HandleCancel);
        }

        private void OnDisable()
        {
            _cancelButton?.onClick.RemoveListener(HandleCancel);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            _searchTime += Time.deltaTime;
            _dotTimer += Time.deltaTime;

            if (_dotTimer >= DotInterval)
            {
                _dotTimer = 0f;
                _dotCount = (_dotCount + 1) % 4;
                UpdateStatusText();
            }

            if (_timerText != null)
            {
                int seconds = Mathf.FloorToInt(_searchTime);
                _timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }

        #endregion

        #region Public Methods

        public void Show()
        {
            gameObject.SetActive(true);
            _searchTime = 0f;
            _dotCount = 0;
            _dotTimer = 0f;
            UpdateStatusText();

            if (_timerText != null)
                _timerText.text = "00:00";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the status text, e.g. when match is found.
        /// </summary>
        public void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        #endregion

        #region Private

        private void UpdateStatusText()
        {
            if (_statusText == null) return;
            string dots = new string('.', _dotCount);
            _statusText.text = $"SEARCHING FOR OPPONENT{dots}";
        }

        private void HandleCancel() => OnCancelMatchmaking?.Invoke();

        #endregion
    }
}
