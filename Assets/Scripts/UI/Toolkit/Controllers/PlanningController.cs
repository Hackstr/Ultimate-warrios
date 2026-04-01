using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.UI.Toolkit
{
    public class PlanningController : UIScreenBase
    {
        public event Action<List<ActionType>> OnActionsConfirmed;
        public event Action OnPassDeviceContinue;

        private VisualElement _queueContainer;
        private VisualElement _passDeviceOverlay;
        private VisualElement _waitingOverlay;
        private Label _timerLabel;
        private Label _roundLabel;
        private Button _btnConfirm;
        private Button _btnUndo;
        private Button _btnPassContinue;

        // Action buttons
        private Button _btnMove;
        private Button _btnTurnLeft;
        private Button _btnTurnRight;
        private Button _btnTurnAround;
        private Button _btnShoot;
        private Button _btnWait;
        private Button _btnSpecial;
        private Button _btnShield;

        private HeroConfig _hero;
        private int _round;
        private readonly List<ActionType> _queue = new();

        public IReadOnlyList<ActionType> CurrentQueue => _queue;
        private int _maxSteps;
        private float _timeRemaining;
        private bool _timerActive;

        protected override void QueryElements()
        {
            _queueContainer = Root.Q("action-queue");
            _passDeviceOverlay = Root.Q("pass-device-overlay");
            _waitingOverlay = Root.Q("waiting-overlay");
            _timerLabel = Root.Q<Label>("timer-label");
            _roundLabel = Root.Q<Label>("round-label");
            _btnConfirm = Root.Q<Button>("btn-confirm");
            _btnUndo = Root.Q<Button>("btn-undo");
            _btnPassContinue = Root.Q<Button>("btn-pass-continue");

            _btnMove = Root.Q<Button>("btn-move");
            _btnTurnLeft = Root.Q<Button>("btn-turn-left");
            _btnTurnRight = Root.Q<Button>("btn-turn-right");
            _btnTurnAround = Root.Q<Button>("btn-turn-around");
            _btnShoot = Root.Q<Button>("btn-shoot");
            _btnWait = Root.Q<Button>("btn-wait");
            _btnSpecial = Root.Q<Button>("btn-special");
            _btnShield = Root.Q<Button>("btn-shield");
        }

        protected override void BindEvents()
        {
            _btnConfirm?.RegisterCallback<ClickEvent>(HandleConfirm);
            _btnUndo?.RegisterCallback<ClickEvent>(HandleUndo);
            _btnPassContinue?.RegisterCallback<ClickEvent>(HandlePassContinue);

            _btnMove?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.Move));
            _btnTurnLeft?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.TurnLeft));
            _btnTurnRight?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.TurnRight));
            _btnTurnAround?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.TurnAround));
            _btnShoot?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.Shoot));
            _btnWait?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.Wait));
            _btnSpecial?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.Special));
            _btnShield?.RegisterCallback<ClickEvent>(_ => TryAddAction(ActionType.Shield));
        }

        protected override void UnbindEvents()
        {
            _btnConfirm?.UnregisterCallback<ClickEvent>(HandleConfirm);
            _btnUndo?.UnregisterCallback<ClickEvent>(HandleUndo);
            _btnPassContinue?.UnregisterCallback<ClickEvent>(HandlePassContinue);
        }

        public void Show(HeroConfig hero, int round, string label = null, float timeLimit = 0f)
        {
            _hero = hero;
            _round = round;
            _maxSteps = hero.steps;
            _queue.Clear();
            _timeRemaining = timeLimit > 0f ? timeLimit : (round == 1 ? 30f : 20f);
            _timerActive = true;

            if (_roundLabel != null)
                _roundLabel.text = label ?? $"ROUND {round} — PLANNING";

            base.Show();
            UpdateQueueDisplay();
            UpdateTimerDisplay();
            UpdateButtonStates();
            HideOverlays();

        }

        public void DisableTimer()
        {
            _timerActive = false;
            if (_timerLabel != null) _timerLabel.text = "∞";
        }

        public void ShowPassDeviceOverlay(string message)
        {
            if (_passDeviceOverlay != null)
            {
                var msg = _passDeviceOverlay.Q<Label>("pass-message");
                if (msg != null) msg.text = message;
                _passDeviceOverlay.AddToClassList("overlay--visible");
            }
        }

        public void ShowWaitingOverlay(string message)
        {
            _timerActive = false;
            if (_waitingOverlay != null)
            {
                var msg = _waitingOverlay.Q<Label>("waiting-message");
                if (msg != null) msg.text = message;
                _waitingOverlay.AddToClassList("overlay--visible");
            }
        }

        public override void Tick(float dt)
        {
            if (!_timerActive) return;

            _timeRemaining -= dt;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _timerActive = false;
                AutoSubmit();
            }
            UpdateTimerDisplay();
        }

        private void TryAddAction(ActionType action)
        {
            if (_hero == null) return;
            int slot = _queue.Count;
            if (slot >= _maxSteps) return;

            if (!ActionValidator.CanPlaceAction(_queue, slot, action, _hero))
                return;

            _queue.Add(action);
            GameEvents.ActionQueued(slot, action);
            UpdateQueueDisplay();
            UpdateButtonStates();
        }

        private void HandleConfirm(ClickEvent _)
        {
            if (_queue.Count < _maxSteps) return;
            _timerActive = false;
            OnActionsConfirmed?.Invoke(new List<ActionType>(_queue));
        }

        private void HandleUndo(ClickEvent _)
        {
            if (_queue.Count == 0) return;
            int idx = _queue.Count - 1;
            _queue.RemoveAt(idx);
            GameEvents.ActionUndone(idx);
            UpdateQueueDisplay();
            UpdateButtonStates();
        }

        private void HandlePassContinue(ClickEvent _)
        {
            HideOverlays();
            OnPassDeviceContinue?.Invoke();
        }

        private void AutoSubmit()
        {
            while (_queue.Count < _maxSteps)
                _queue.Add(ActionType.Wait);

            GameEvents.PlanningTimeExpired();
            OnActionsConfirmed?.Invoke(new List<ActionType>(_queue));
        }

        private void UpdateQueueDisplay()
        {
            if (_queueContainer == null) return;
            _queueContainer.Clear();

            for (int i = 0; i < _maxSteps; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("action-slot");

                if (i < _queue.Count)
                {
                    slot.AddToClassList("action-slot--filled");
                    var icon = new Label(GetActionIcon(_queue[i]));
                    icon.AddToClassList("action-slot__icon");
                    slot.Add(icon);

                    var label = new Label(GetActionLabel(_queue[i]));
                    label.AddToClassList("action-slot__label");
                    slot.Add(label);

                    // Pop-in animation for the last added slot
                    if (i == _queue.Count - 1)
                    {
                        slot.style.scale = new StyleScale(new Scale(new Vector3(0.5f, 0.5f, 1f)));
                        slot.schedule.Execute(() =>
                        {
                            slot.style.scale = new StyleScale(new Scale(Vector3.one));
                        }).ExecuteLater(16);
                    }
                }
                else if (i == _queue.Count)
                {
                    slot.AddToClassList("action-slot--active");
                    var label = new Label($"{i + 1}");
                    label.AddToClassList("action-slot__label");
                    slot.Add(label);
                }
                else
                {
                    var label = new Label($"{i + 1}");
                    label.AddToClassList("action-slot__label");
                    slot.Add(label);
                }

                _queueContainer.Add(slot);
            }
        }

        private void UpdateButtonStates()
        {
            if (_hero == null) return;
            int nextSlot = _queue.Count;
            bool full = nextSlot >= _maxSteps;

            SetButtonEnabled(_btnMove, !full);
            SetButtonEnabled(_btnTurnLeft, !full);
            SetButtonEnabled(_btnTurnRight, !full);
            SetButtonEnabled(_btnTurnAround, !full);
            SetButtonEnabled(_btnWait, !full);
            SetButtonEnabled(_btnShoot, !full && ActionValidator.CanPlaceAction(_queue, nextSlot, ActionType.Shoot, _hero));
            SetButtonEnabled(_btnShield, !full);
            SetButtonEnabled(_btnSpecial, !full && ActionValidator.CanPlaceAction(_queue, nextSlot, ActionType.Special, _hero));
            SetButtonEnabled(_btnConfirm, _queue.Count >= _maxSteps);
            SetButtonEnabled(_btnUndo, _queue.Count > 0);

            if (_btnConfirm != null)
                _btnConfirm.text = _queue.Count >= _maxSteps ? "CONFIRM" : $"{_queue.Count}/{_maxSteps}";
        }

        private static void SetButtonEnabled(Button btn, bool enabled)
        {
            if (btn == null) return;
            btn.SetEnabled(enabled);
            if (enabled)
                btn.RemoveFromClassList("btn--disabled");
            else
                btn.AddToClassList("btn--disabled");
        }

        private void UpdateTimerDisplay()
        {
            if (_timerLabel == null) return;
            int seconds = Mathf.CeilToInt(_timeRemaining);
            _timerLabel.text = seconds.ToString();

            GameEvents.PlanningTimerTick(seconds);

            // Use USS classes instead of inline colors
            _timerLabel.RemoveFromClassList("timer--danger");
            _timerLabel.RemoveFromClassList("timer--warning");
            _timerLabel.RemoveFromClassList("timer--normal");

            if (_timeRemaining <= 5f)
            {
                _timerLabel.AddToClassList("timer--danger");
                // Pulse scale when danger
                bool pulse = ((int)(_timeRemaining * 2f)) % 2 == 0;
                float s = pulse ? 1.15f : 1f;
                _timerLabel.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
            }
            else
            {
                _timerLabel.style.scale = new StyleScale(new Scale(Vector3.one));
                if (_timeRemaining <= 10f)
                    _timerLabel.AddToClassList("timer--warning");
                else
                    _timerLabel.AddToClassList("timer--normal");
            }
        }

        private void HideOverlays()
        {
            _passDeviceOverlay?.RemoveFromClassList("overlay--visible");
            _waitingOverlay?.RemoveFromClassList("overlay--visible");
        }

        private static string GetActionIcon(ActionType action) => action switch
        {
            ActionType.Move => "^",
            ActionType.TurnLeft => "<",
            ActionType.TurnRight => ">",
            ActionType.TurnAround => "v",
            ActionType.Shoot => "X",
            ActionType.Wait => "||",
            ActionType.Special => "*",
            ActionType.Shield => "O",
            _ => "?"
        };

        private static string GetActionLabel(ActionType action) => action switch
        {
            ActionType.Move => "MOVE",
            ActionType.TurnLeft => "LEFT",
            ActionType.TurnRight => "RIGHT",
            ActionType.TurnAround => "180",
            ActionType.Shoot => "SHOOT",
            ActionType.Wait => "WAIT",
            ActionType.Special => "SPEC",
            ActionType.Shield => "SHLD",
            _ => "?"
        };
    }
}
