using System;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    public class MatchmakingController : UIScreenBase
    {
        public event Action OnCancelMatchmaking;

        private Label _statusText;
        private Label _timerText;
        private Label _searchDots;
        private Button _btnCancel;

        private float _elapsed;
        private int _dotFrame;
        private const float DOT_INTERVAL = 0.4f;
        private float _dotTimer;

        protected override void QueryElements()
        {
            _statusText = Root.Q<Label>("status-text");
            _timerText = Root.Q<Label>("timer-text");
            _searchDots = Root.Q<Label>("search-dots");
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
            _elapsed = 0f;
            _dotFrame = 0;
            _dotTimer = 0f;
            SetStatus("FINDING OPPONENT");
        }

        public void SetStatus(string status)
        {
            if (_statusText != null) _statusText.text = status;
        }

        public override void Tick(float dt)
        {
            _elapsed += dt;
            var minutes = (int)(_elapsed / 60f);
            var seconds = (int)(_elapsed % 60f);
            if (_timerText != null)
                _timerText.text = $"{minutes}:{seconds:D2}";

            _dotTimer += dt;
            if (_dotTimer >= DOT_INTERVAL)
            {
                _dotTimer = 0f;
                _dotFrame = (_dotFrame + 1) % 4;
                if (_searchDots != null)
                {
                    var dots = _dotFrame switch
                    {
                        0 => "●  ●  ●  ○  ○",
                        1 => "○  ●  ●  ●  ○",
                        2 => "○  ○  ●  ●  ●",
                        _ => "●  ○  ○  ●  ●"
                    };
                    _searchDots.text = dots;
                }
            }
        }

        private void HandleCancel(ClickEvent _) => OnCancelMatchmaking?.Invoke();
    }
}
