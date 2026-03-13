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
        [SerializeField] private TextMeshProUGUI _stepText;
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private TextMeshProUGUI _lockText;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new(0.18f, 0.18f, 0.24f, 0.7f);
        [SerializeField] private Color _moveColor = new(0.15f, 0.40f, 0.60f, 1f);
        [SerializeField] private Color _turnColor = new(0.40f, 0.40f, 0.18f, 1f);
        [SerializeField] private Color _cooldownColor = new(0.35f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color _shootColor = new(0.75f, 0.20f, 0.20f, 1f);
        [SerializeField] private Color _specialColor = new(0.55f, 0.25f, 0.75f, 1f);
        [SerializeField] private Color _waitColor = new(0.30f, 0.30f, 0.35f, 0.9f);
        [SerializeField] private Color _highlightColor = new(0.30f, 0.50f, 0.80f, 0.8f);

        #endregion

        #region Public Methods

        public void SetStepNumber(int step)
        {
            if (_stepText != null)
                _stepText.text = $"#{step}";
        }

        /// <summary>
        /// Shows a filled slot with the given action.
        /// </summary>
        public void SetAction(ActionType action, string icon)
        {
            HideLock();

            if (_iconText != null)
            {
                _iconText.text = icon;
                _iconText.color = Color.white;
            }

            if (_labelText != null)
            {
                _labelText.text = GetLabel(action);
                _labelText.color = Color.white;
            }

            if (_stepText != null)
                _stepText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            if (_background != null)
                _background.color = GetColorForAction(action);
        }

        /// <summary>
        /// Shows an empty waiting slot — dimmed out.
        /// </summary>
        public void SetEmpty()
        {
            HideLock();

            if (_iconText != null)
            {
                _iconText.text = "...";
                _iconText.color = new Color(0.4f, 0.4f, 0.5f, 1f);
            }

            if (_labelText != null)
            {
                _labelText.text = "";
                _labelText.color = new Color(0.4f, 0.4f, 0.5f, 1f);
            }

            if (_stepText != null)
                _stepText.color = new Color(0.35f, 0.35f, 0.4f, 1f);

            if (_background != null)
                _background.color = _emptyColor;
        }

        /// <summary>
        /// Highlights this slot as the next one to fill — pulsing border.
        /// </summary>
        public void SetHighlighted()
        {
            HideLock();

            if (_iconText != null)
            {
                _iconText.text = "?";
                _iconText.color = new Color(0.6f, 0.8f, 1f, 1f);
            }

            if (_labelText != null)
            {
                _labelText.text = "Choose action...";
                _labelText.color = new Color(0.6f, 0.8f, 1f, 1f);
            }

            if (_stepText != null)
                _stepText.color = Color.white;

            if (_background != null)
                _background.color = _highlightColor;
        }

        /// <summary>
        /// Shows a locked slot (e.g. under cooldown).
        /// </summary>
        public void SetCooldownLock(int turnsRemaining)
        {
            if (_iconText != null)
            {
                _iconText.text = "X";
                _iconText.color = new Color(0.8f, 0.4f, 0.4f, 1f);
            }

            if (_labelText != null)
            {
                _labelText.text = "Cooldown";
                _labelText.color = new Color(0.8f, 0.4f, 0.4f, 1f);
            }

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
            ActionType.Move => _moveColor,
            ActionType.TurnLeft => _turnColor,
            ActionType.TurnRight => _turnColor,
            ActionType.TurnAround => _turnColor,
            ActionType.Wait => _waitColor,
            _ => _moveColor
        };

        private static string GetLabel(ActionType action) => action switch
        {
            ActionType.Move => "MOVE",
            ActionType.TurnLeft => "LEFT",
            ActionType.TurnRight => "RIGHT",
            ActionType.TurnAround => "180",
            ActionType.Shoot => "SHOOT",
            ActionType.Wait => "WAIT",
            ActionType.Special => "SPL",
            _ => ""
        };

        #endregion
    }
}
