using System;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Localization;

namespace TacticalDuelist.UI.Toolkit
{
    public class ReconnectingController : UIScreenBase
    {
        public event Action OnCancelReconnect;

        private Label _titleText;
        private Label _attemptText;
        private Label _retryDots;
        private Label _infoText;
        private Button _btnCancel;

        private int _dotFrame;
        private float _dotTimer;
        private const float DOT_INTERVAL = 0.35f;

        private int _currentAttempt;
        private int _maxAttempts;

        protected override void QueryElements()
        {
            _titleText = Root.Q<Label>("title-text");
            _attemptText = Root.Q<Label>("attempt-text");
            _retryDots = Root.Q<Label>("retry-dots");
            _infoText = Root.Q<Label>("info-text");
            _btnCancel = Root.Q<Button>("btn-cancel");
        }

        protected override void BindEvents()
        {
            _btnCancel?.RegisterCallback<ClickEvent>(HandleCancel);
        }

        protected override void UnbindEvents()
        {
            _btnCancel?.UnregisterCallback<ClickEvent>(HandleCancel);
        }

        protected override void OnShow()
        {
            _dotFrame = 0;
            _dotTimer = 0f;
            SetTitle(L.Get("connection_lost"));
        }

        public void SetTitle(string title)
        {
            if (_titleText != null) _titleText.text = title;
        }

        public void SetAttempt(int current, int max)
        {
            _currentAttempt = current;
            _maxAttempts = max;
            if (_attemptText != null)
                _attemptText.text = L.Get("reconnecting", current, max);
        }

        public void SetFailed()
        {
            SetTitle(L.Get("reconnect_failed"));
            if (_attemptText != null)
                _attemptText.text = L.Get("cannot_reach");
            if (_infoText != null)
                _infoText.text = L.Get("match_forfeited");
        }

        public override void Tick(float dt)
        {
            _dotTimer += dt;
            if (_dotTimer >= DOT_INTERVAL)
            {
                _dotTimer = 0f;
                _dotFrame = (_dotFrame + 1) % 4;
                if (_retryDots != null)
                {
                    _retryDots.text = _dotFrame switch
                    {
                        0 => "●  ●  ●  ○  ○",
                        1 => "○  ●  ●  ●  ○",
                        2 => "○  ○  ●  ●  ●",
                        _ => "●  ○  ○  ●  ●"
                    };
                }
            }
        }

        private void HandleCancel(ClickEvent _) => OnCancelReconnect?.Invoke();
    }
}
