using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Individual hero card in the selection roster.
    /// Shows portrait, name, and handles tap to select.
    /// </summary>
    public class HeroCardUI : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image _portrait;
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Button _button;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new(0.15f, 0.15f, 0.2f, 1f);
        [SerializeField] private Color _highlightColor = new(0.3f, 0.5f, 1f, 1f);
        [SerializeField] private Color _borderNormal = new(0.3f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color _borderHighlight = new(1f, 0.85f, 0.2f, 1f);

        #endregion

        #region Fields

        private HeroConfig _hero;
        private Action<HeroConfig> _onClicked;

        #endregion

        #region Properties

        public HeroConfig Hero => _hero;

        #endregion

        #region Public Methods

        public void Setup(HeroConfig hero, Action<HeroConfig> onClick)
        {
            _hero = hero;
            _onClicked = onClick;

            if (_nameText != null)
                _nameText.text = hero.displayName;

            if (_portrait != null && hero.portrait != null)
                _portrait.sprite = hero.portrait;

            if (_background != null)
                _background.color = _normalColor;

            _button?.onClick.RemoveAllListeners();
            _button?.onClick.AddListener(HandleClick);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_background != null)
                _background.color = highlighted ? _highlightColor : _normalColor;

            if (_border != null)
                _border.color = highlighted ? _borderHighlight : _borderNormal;
        }

        #endregion

        #region Private

        private void HandleClick()
        {
            _onClicked?.Invoke(_hero);
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveAllListeners();
        }

        #endregion
    }
}
