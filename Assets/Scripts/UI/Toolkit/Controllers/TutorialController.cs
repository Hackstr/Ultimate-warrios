using System;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    public class TutorialController : UIScreenBase
    {
        public event Action OnComplete;
        public event Action OnSkip;

        private VisualElement[] _slides;
        private Button _btnNext;
        private Button _btnSkip;
        private Label _btnNextLabel;
        private int _currentSlide;

        protected override void QueryElements()
        {
            _slides = new VisualElement[3];
            _slides[0] = Root.Q("slide-0");
            _slides[1] = Root.Q("slide-1");
            _slides[2] = Root.Q("slide-2");

            _btnNext = Root.Q<Button>("btn-next");
            _btnSkip = Root.Q<Button>("btn-skip");
            _btnNextLabel = Root.Q<Label>("btn-next-label");
        }

        protected override void BindEvents()
        {
            _btnNext?.RegisterCallback<ClickEvent>(HandleNext);
            _btnSkip?.RegisterCallback<ClickEvent>(HandleSkip);
        }

        protected override void UnbindEvents()
        {
            _btnNext?.UnregisterCallback<ClickEvent>(HandleNext);
            _btnSkip?.UnregisterCallback<ClickEvent>(HandleSkip);
        }

        protected override void OnShow()
        {
            _currentSlide = 0;
            UpdateSlides();
        }

        private void HandleNext(ClickEvent _)
        {
            if (_currentSlide < 2)
            {
                _currentSlide++;
                UpdateSlides();
            }
            else
            {
                OnComplete?.Invoke();
            }
        }

        private void HandleSkip(ClickEvent _)
        {
            OnSkip?.Invoke();
        }

        private void UpdateSlides()
        {
            for (int i = 0; i < _slides.Length; i++)
            {
                if (_slides[i] != null)
                    _slides[i].style.display = i == _currentSlide
                        ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_btnNextLabel != null)
                _btnNextLabel.text = _currentSlide == 2 ? "START MATCH" : "NEXT";
        }
    }
}
