using System;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Localization;

namespace TacticalDuelist.UI.Toolkit
{
    public class SplashController : UIScreenBase
    {
        public event Action OnSplashComplete;

        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _loadingLabel;
        private VisualElement _logoContainer;

        private float _elapsed;
        private const float SPLASH_DURATION = 2.5f;
        private bool _complete;

        protected override void QueryElements()
        {
            _titleLabel = Root.Q<Label>("splash-title");
            _subtitleLabel = Root.Q<Label>("splash-subtitle");
            _loadingLabel = Root.Q<Label>("splash-loading");
            _logoContainer = Root.Q("splash-logo");
        }

        protected override void BindEvents() { }
        protected override void UnbindEvents() { }

        protected override void OnShow()
        {
            _elapsed = 0f;
            _complete = false;
        }

        public override void Tick(float dt)
        {
            if (_complete) return;

            _elapsed += dt;

            // Animate loading dots
            if (_loadingLabel != null)
            {
                int dots = ((int)(_elapsed * 3f)) % 4;
                _loadingLabel.text = L.Get("loading") + new string('.', dots);
            }

            if (_elapsed >= SPLASH_DURATION)
            {
                _complete = true;
                OnSplashComplete?.Invoke();
            }
        }
    }
}
