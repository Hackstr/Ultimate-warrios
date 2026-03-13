using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.UI
{
    /// <summary>
    /// Planning phase UI. Shows action queue slots, action buttons with cooldown state,
    /// countdown timer, and path preview. Validates actions via ActionValidator.
    /// </summary>
    public class PlanningScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Action Buttons")]
        [SerializeField] private Button _moveButton;
        [SerializeField] private Button _turnLeftButton;
        [SerializeField] private Button _turnRightButton;
        [SerializeField] private Button _turnAroundButton;
        [SerializeField] private Button _shootButton;
        [SerializeField] private Button _waitButton;
        [SerializeField] private Button _specialButton;

        [Header("Shoot Cooldown Overlay")]
        [SerializeField] private GameObject _shootCooldownOverlay;
        [SerializeField] private TextMeshProUGUI _shootCooldownText;

        [Header("Special State")]
        [SerializeField] private GameObject _specialLockOverlay;

        [Header("Queue Display")]
        [SerializeField] private Transform _queueContainer;
        [SerializeField] private GameObject _queueSlotPrefab;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _timerBackground;
        [SerializeField] private Color _timerNormalColor = Color.white;
        [SerializeField] private Color _timerWarningColor = Color.yellow;
        [SerializeField] private Color _timerDangerColor = Color.red;

        [Header("Confirm / Undo")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;

        [Header("Player Indicator")]
        [SerializeField] private TextMeshProUGUI _playerLabel;

        [Header("Pass Device (offline)")]
        [SerializeField] private GameObject _passDeviceOverlay;
        [SerializeField] private Button _passDeviceTapButton;
        [SerializeField] private TextMeshProUGUI _passDeviceMessage;

        [Header("Timer Settings")]
        [SerializeField] private float _roundTimerDuration = 30f;
        [SerializeField] private float _laterRoundTimerDuration = 20f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when player confirms their action sequence.
        /// </summary>
        public event Action<List<ActionType>> OnActionsConfirmed;

        /// <summary>
        /// Fired when user taps through the "pass device" overlay.
        /// </summary>
        public event Action OnPassDeviceContinue;

        #endregion

        #region Fields

        private HeroConfig _currentHero;
        private readonly List<ActionType> _actionQueue = new();
        private readonly List<ActionSlotUI> _slots = new();
        private float _timeRemaining;
        private bool _timerActive;
        private bool _confirmed;
        private int _currentRound;

        #endregion

        #region Properties

        public IReadOnlyList<ActionType> CurrentQueue => _actionQueue;
        public bool IsConfirmed => _confirmed;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _moveButton?.onClick.AddListener(() => TryAddAction(ActionType.Move));
            _turnLeftButton?.onClick.AddListener(() => TryAddAction(ActionType.TurnLeft));
            _turnRightButton?.onClick.AddListener(() => TryAddAction(ActionType.TurnRight));
            _turnAroundButton?.onClick.AddListener(() => TryAddAction(ActionType.TurnAround));
            _shootButton?.onClick.AddListener(() => TryAddAction(ActionType.Shoot));
            _waitButton?.onClick.AddListener(() => TryAddAction(ActionType.Wait));
            _specialButton?.onClick.AddListener(() => TryAddAction(ActionType.Special));

            _confirmButton?.onClick.AddListener(OnConfirmPressed);
            _undoButton?.onClick.AddListener(OnUndoPressed);
            _passDeviceTapButton?.onClick.AddListener(OnPassDeviceTapped);
        }

        private void OnDisable()
        {
            _moveButton?.onClick.RemoveAllListeners();
            _turnLeftButton?.onClick.RemoveAllListeners();
            _turnRightButton?.onClick.RemoveAllListeners();
            _turnAroundButton?.onClick.RemoveAllListeners();
            _shootButton?.onClick.RemoveAllListeners();
            _waitButton?.onClick.RemoveAllListeners();
            _specialButton?.onClick.RemoveAllListeners();

            _confirmButton?.onClick.RemoveAllListeners();
            _undoButton?.onClick.RemoveAllListeners();
            _passDeviceTapButton?.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (!_timerActive || _confirmed) return;

            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _timerActive = false;
                AutoSubmit();
            }

            UpdateTimerDisplay();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens planning for a given hero and round.
        /// </summary>
        public void Show(HeroConfig hero, int round, string playerLabel = null)
        {
            gameObject.SetActive(true);
            _currentHero = hero;
            _currentRound = round;
            _confirmed = false;
            _actionQueue.Clear();

            if (_playerLabel != null)
                _playerLabel.text = playerLabel ?? $"Round {round} — {hero.displayName}";

            BuildQueueSlots();
            UpdateAllButtonStates();
            UpdateConfirmButton();

            _timeRemaining = round <= 1 ? _roundTimerDuration : _laterRoundTimerDuration;
            _timerActive = true;

            _confirmButton.interactable = false;
            _undoButton.interactable = false;
        }

        public void Hide()
        {
            _timerActive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows a simplified overlay with a waiting message (e.g., after submitting actions).
        /// </summary>
        public void ShowWaitingOverlay(string message)
        {
            gameObject.SetActive(true);
            _timerActive = false;
            _confirmed = true;

            if (_playerLabel != null)
                _playerLabel.text = message;

            if (_confirmButton != null)
                _confirmButton.interactable = false;
            if (_undoButton != null)
                _undoButton.interactable = false;

            SetActionButtonsInteractable(false);
        }

        /// <summary>
        /// Returns current queue padded with Wait actions.
        /// </summary>
        public List<ActionType> GetPaddedActions()
        {
            return ActionValidator.PadWithWait(
                new List<ActionType>(_actionQueue), _currentHero.steps);
        }

        #endregion

        #region Private — Queue Management

        private void TryAddAction(ActionType action)
        {
            if (_confirmed || _currentHero == null) return;

            int slot = _actionQueue.Count;
            if (slot >= _currentHero.steps) return;

            if (!ActionValidator.CanPlaceAction(_actionQueue, slot, action, _currentHero))
                return;

            _actionQueue.Add(action);
            GameEvents.ActionQueued(slot, action);

            RefreshSlots();
            UpdateAllButtonStates();
            UpdateConfirmButton();
        }

        private void OnUndoPressed()
        {
            if (_confirmed || _actionQueue.Count == 0) return;

            int removedIndex = _actionQueue.Count - 1;
            _actionQueue.RemoveAt(removedIndex);
            GameEvents.ActionUndone(removedIndex);

            RefreshSlots();
            UpdateAllButtonStates();
            UpdateConfirmButton();
        }

        private void OnConfirmPressed()
        {
            if (_confirmed) return;
            if (_actionQueue.Count < _currentHero.steps) return;

            var error = ActionValidator.Validate(_actionQueue, _currentHero);
            if (error != null)
            {
                Debug.LogWarning($"[PlanningScreen] Validation failed: {error}");
                return;
            }

            _confirmed = true;
            _timerActive = false;
            OnActionsConfirmed?.Invoke(new List<ActionType>(_actionQueue));
        }

        private void AutoSubmit()
        {
            var padded = GetPaddedActions();
            _confirmed = true;
            GameEvents.PlanningTimeExpired();
            OnActionsConfirmed?.Invoke(padded);
        }

        #endregion

        #region Private — Queue Slot Display

        private void BuildQueueSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null && slot.gameObject != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (_queueSlotPrefab == null || _queueContainer == null) return;

            for (int i = 0; i < _currentHero.steps; i++)
            {
                var slotObj = Instantiate(_queueSlotPrefab, _queueContainer);
                slotObj.SetActive(true);

                // Template has ignoreLayout=true; clones must participate in layout
                var le = slotObj.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le != null) le.ignoreLayout = false;

                var slotUI = slotObj.GetComponent<ActionSlotUI>();
                if (slotUI != null)
                {
                    slotUI.SetStepNumber(i + 1);
                    _slots.Add(slotUI);
                }
            }

            RefreshSlots();
        }

        private void RefreshSlots()
        {
            int cooldown = 0;
            int nextEmptySlot = _actionQueue.Count;
            bool highlightedOne = false;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < _actionQueue.Count)
                {
                    var action = _actionQueue[i];
                    _slots[i].SetAction(action, GetActionIcon(action));

                    if (action == ActionType.Shoot)
                        cooldown = _currentHero.cooldown;
                    else
                        cooldown = cooldown > 0 ? cooldown - 1 : 0;
                }
                else if (cooldown > 0)
                {
                    _slots[i].SetCooldownLock(cooldown);
                    cooldown--;
                }
                else if (!highlightedOne && !_confirmed)
                {
                    _slots[i].SetHighlighted();
                    highlightedOne = true;
                }
                else
                {
                    _slots[i].SetEmpty();
                }
            }

            _undoButton.interactable = _actionQueue.Count > 0 && !_confirmed;
        }

        #endregion

        #region Private — Button State

        private void UpdateAllButtonStates()
        {
            int nextSlot = _actionQueue.Count;
            bool queueFull = nextSlot >= _currentHero.steps;
            bool locked = _confirmed || queueFull;

            _moveButton.interactable = !locked;
            _turnLeftButton.interactable = !locked;
            _turnRightButton.interactable = !locked;
            _turnAroundButton.interactable = !locked;
            _waitButton.interactable = !locked;

            bool canShoot = !locked && ActionValidator.CanPlaceAction(_actionQueue, nextSlot, ActionType.Shoot, _currentHero);
            _shootButton.interactable = canShoot;
            UpdateShootCooldownOverlay(nextSlot);

            bool canSpecial = !locked && ActionValidator.CanPlaceAction(_actionQueue, nextSlot, ActionType.Special, _currentHero);
            _specialButton.interactable = canSpecial;
            if (_specialLockOverlay != null)
                _specialLockOverlay.SetActive(!canSpecial && !locked);
        }

        private void UpdateShootCooldownOverlay(int nextSlot)
        {
            if (_shootCooldownOverlay == null) return;

            int cd = CalculateCooldownAtSlot(nextSlot);
            bool onCooldown = cd > 0;

            _shootCooldownOverlay.SetActive(onCooldown);
            if (onCooldown && _shootCooldownText != null)
                _shootCooldownText.text = cd.ToString();
        }

        private int CalculateCooldownAtSlot(int slot)
        {
            int cd = 0;
            for (int i = 0; i < slot && i < _actionQueue.Count; i++)
            {
                if (_actionQueue[i] == ActionType.Shoot)
                    cd = _currentHero.cooldown;
                else
                    cd = cd > 0 ? cd - 1 : 0;
            }
            return cd;
        }

        private void UpdateConfirmButton()
        {
            bool ready = _actionQueue.Count >= _currentHero.steps && !_confirmed;
            _confirmButton.interactable = ready;

            if (_confirmButtonText != null)
                _confirmButtonText.text = ready ? "CONFIRM" : $"{_actionQueue.Count}/{_currentHero.steps}";
        }

        #endregion

        #region Private — Timer

        private void UpdateTimerDisplay()
        {
            int seconds = Mathf.CeilToInt(_timeRemaining);

            if (_timerText != null)
                _timerText.text = seconds.ToString();

            if (_timerBackground != null)
            {
                if (_timeRemaining > 10f)
                    _timerBackground.color = _timerNormalColor;
                else if (_timeRemaining > 5f)
                    _timerBackground.color = _timerWarningColor;
                else
                    _timerBackground.color = _timerDangerColor;
            }

            if (_timeRemaining <= 5f && _timerText != null)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                _timerText.transform.localScale = Vector3.one * (1f + pulse * 0.15f);
            }
            else if (_timerText != null)
            {
                _timerText.transform.localScale = Vector3.one;
            }

            GameEvents.PlanningTimerTick(seconds);
        }

        #endregion

        #region Private — Helpers

        private void SetActionButtonsInteractable(bool interactable)
        {
            if (_moveButton != null) _moveButton.interactable = interactable;
            if (_turnLeftButton != null) _turnLeftButton.interactable = interactable;
            if (_turnRightButton != null) _turnRightButton.interactable = interactable;
            if (_turnAroundButton != null) _turnAroundButton.interactable = interactable;
            if (_shootButton != null) _shootButton.interactable = interactable;
            if (_waitButton != null) _waitButton.interactable = interactable;
            if (_specialButton != null) _specialButton.interactable = interactable;
        }

        private static string GetActionIcon(ActionType action) => action switch
        {
            ActionType.Move => ">>",
            ActionType.TurnLeft => "<<",
            ActionType.TurnRight => ">>",
            ActionType.TurnAround => "<>",
            ActionType.Shoot => "X",
            ActionType.Wait => "--",
            ActionType.Special => "*",
            _ => "?"
        };

        #endregion

        #region Pass Device Overlay

        /// <summary>
        /// Shows a full-screen overlay telling players to pass the device (offline mode).
        /// </summary>
        public void ShowPassDeviceOverlay(string message = null)
        {
            if (_passDeviceOverlay == null) return;

            gameObject.SetActive(true);
            _timerActive = false;
            _passDeviceOverlay.SetActive(true);

            if (_passDeviceMessage != null)
                _passDeviceMessage.text = message ?? "Pass the device\nto the other player";
        }

        public void HidePassDeviceOverlay()
        {
            if (_passDeviceOverlay != null)
                _passDeviceOverlay.SetActive(false);
        }

        private void OnPassDeviceTapped()
        {
            HidePassDeviceOverlay();
            OnPassDeviceContinue?.Invoke();
        }

        #endregion
    }
}
