using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Represents a single action slot in the planning queue.
    /// Displays icon, label, and cooldown lock state.
    /// </summary>
    public class ActionSlotUI : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image _background;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private TextMeshProUGUI _lockText;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new(0.2f, 0.2f, 0.25f, 0.5f);
        [SerializeField] private Color _filledColor = new(0.15f, 0.3f, 0.5f, 1f);
        [SerializeField] private Color _cooldownColor = new(0.3f, 0.15f, 0.15f, 0.6f);
        [SerializeField] private Color _shootColor = new(0.7f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _specialColor = new(0.6f, 0.3f, 0.8f, 1f);

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a filled slot with the given action.
        /// </summary>
        public void SetAction(ActionType action, string icon)
        {
            HideLock();

            if (_iconText != null)
                _iconText.text = icon;

            if (_labelText != null)
                _labelText.text = GetLabel(action);

            if (_background != null)
                _background.color = GetColorForAction(action);
        }

        /// <summary>
        /// Shows an empty waiting slot.
        /// </summary>
        public void SetEmpty()
        {
            HideLock();

            if (_iconText != null)
                _iconText.text = "";

            if (_labelText != null)
                _labelText.text = "";

            if (_background != null)
                _background.color = _emptyColor;
        }

        /// <summary>
        /// Shows a locked slot (e.g. under cooldown).
        /// </summary>
        public void SetCooldownLock(int turnsRemaining)
        {
            if (_iconText != null)
                _iconText.text = "";

            if (_labelText != null)
                _labelText.text = "";

            if (_background != null)
                _background.color = _cooldownColor;

            if (_lockOverlay != null)
                _lockOverlay.SetActive(true);

            if (_lockText != null)
                _lockText.text = $"CD:{turnsRemaining}";
        }

        #endregion

        #region Private

        private void HideLock()
        {
            if (_lockOverlay != null)
                _lockOverlay.SetActive(false);
        }

        private Color GetColorForAction(ActionType action) => action switch
        {
            ActionType.Shoot => _shootColor,
            ActionType.Special => _specialColor,
            _ => _filledColor
        };

        private static string GetLabel(ActionType action) => action switch
        {
            ActionType.Move => "MOVE",
            ActionType.TurnLeft => "LEFT",
            ActionType.TurnRight => "RIGHT",
            ActionType.TurnAround => "TURN",
            ActionType.Shoot => "SHOOT",
            ActionType.Wait => "WAIT",
            ActionType.Special => "SPECIAL",
            _ => ""
        };

        #endregion
    }
}
