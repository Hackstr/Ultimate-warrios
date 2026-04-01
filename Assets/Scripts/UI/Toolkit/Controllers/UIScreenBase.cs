using UnityEngine;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    /// <summary>
    /// Base class for all UI Toolkit screen controllers.
    /// Supports fade + slide-up transition on show, fade-out on hide.
    /// </summary>
    public abstract class UIScreenBase
    {
        protected VisualElement Root { get; private set; }

        public bool IsVisible { get; private set; }

        /// <summary>
        /// Transition type for screen entrance.
        /// Override in subclass to change behavior.
        /// </summary>
        protected virtual ScreenTransition Transition => ScreenTransition.Fade;

        private const int TransitionDelay = 16; // ms — one frame before triggering CSS transition
        private const int TransitionDuration = 300; // ms — matches USS .screen-wrapper

        public void Bind(VisualElement root)
        {
            Root = root;
            if (Root != null) Root.pickingMode = PickingMode.Ignore;
            QueryElements();
            BindEvents();
        }

        public virtual void Show()
        {
            if (IsVisible) return;
            IsVisible = true;

            if (Root != null)
            {
                Root.AddToClassList("screen--visible");
                Root.pickingMode = PickingMode.Position;

                // Set initial state (before CSS transition kicks in)
                Root.style.opacity = 0f;
                Root.style.scale = new StyleScale(new Scale(Vector3.one));

                switch (Transition)
                {
                    case ScreenTransition.Fade:
                        Root.style.translate = StyleKeyword.None;
                        break;
                    case ScreenTransition.SlideUp:
                        Root.style.translate = new Translate(0, 80, 0);
                        break;
                    case ScreenTransition.SlideLeft:
                        Root.style.translate = new Translate(120, 0, 0);
                        break;
                    case ScreenTransition.SlideRight:
                        Root.style.translate = new Translate(-120, 0, 0);
                        break;
                    case ScreenTransition.ScaleFade:
                        Root.style.translate = StyleKeyword.None;
                        Root.style.scale = new StyleScale(new Scale(new Vector3(0.92f, 0.92f, 1f)));
                        break;
                    case ScreenTransition.None:
                        Root.style.opacity = 1f;
                        Root.style.translate = StyleKeyword.None;
                        OnShow();
                        return;
                }

                // Trigger CSS transition on next frame
                Root.schedule.Execute(() =>
                {
                    if (Root == null) return;
                    Root.style.opacity = 1f;
                    Root.style.translate = new Translate(0, 0, 0);
                    Root.style.scale = new StyleScale(new Scale(Vector3.one));
                }).ExecuteLater(TransitionDelay);
            }

            OnShow();
        }

        public virtual void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;

            if (Root != null)
            {
                Root.pickingMode = PickingMode.Ignore;

                // Animate out
                Root.style.opacity = 0f;
                switch (Transition)
                {
                    case ScreenTransition.SlideUp:
                        Root.style.translate = new Translate(0, 40, 0);
                        break;
                    case ScreenTransition.SlideLeft:
                        Root.style.translate = new Translate(-60, 0, 0);
                        break;
                    case ScreenTransition.SlideRight:
                        Root.style.translate = new Translate(60, 0, 0);
                        break;
                    case ScreenTransition.ScaleFade:
                        Root.style.scale = new StyleScale(new Scale(new Vector3(0.95f, 0.95f, 1f)));
                        break;
                }

                Root.schedule.Execute(() =>
                {
                    if (Root != null && !IsVisible)
                    {
                        Root.RemoveFromClassList("screen--visible");
                        Root.style.translate = StyleKeyword.None;
                        Root.style.scale = new StyleScale(new Scale(Vector3.one));
                    }
                }).ExecuteLater(TransitionDuration);
            }

            OnHide();
        }

        public virtual void Tick(float deltaTime) { }

        public virtual void Dispose()
        {
            UnbindEvents();
        }

        protected abstract void QueryElements();
        protected abstract void BindEvents();
        protected abstract void UnbindEvents();
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }

    public enum ScreenTransition
    {
        None,
        Fade,
        SlideUp,
        SlideLeft,
        SlideRight,
        ScaleFade
    }
}
