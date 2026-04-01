using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    public enum ToastType { Success, Error, Warning, Info }

    public class ToastManager : UIScreenBase
    {
        private VisualElement _container;
        private readonly List<ToastEntry> _active = new();

        private const float TOAST_DURATION = 3f;

        protected override void QueryElements()
        {
            _container = Root.Q("toast-container");
        }

        protected override void BindEvents() { }
        protected override void UnbindEvents() { }

        public void ShowToast(string message, ToastType type = ToastType.Info)
        {
            if (_container == null) return;

            var toast = new VisualElement();
            toast.AddToClassList("toast");
            toast.AddToClassList(type switch
            {
                ToastType.Success => "toast--success",
                ToastType.Error => "toast--error",
                ToastType.Warning => "toast--warning",
                _ => "toast--info"
            });

            var icon = new Label(type switch
            {
                ToastType.Success => "V",
                ToastType.Error => "X",
                ToastType.Warning => "!",
                _ => "i"
            });
            icon.AddToClassList("toast__icon");
            icon.AddToClassList(type switch
            {
                ToastType.Success => "toast__icon--success",
                ToastType.Error => "toast__icon--error",
                ToastType.Warning => "toast__icon--warning",
                _ => "toast__icon--info"
            });

            var text = new Label(message);
            text.AddToClassList("toast__text");

            toast.Add(icon);
            toast.Add(text);
            _container.Add(toast);

            _active.Add(new ToastEntry { Element = toast, TimeLeft = TOAST_DURATION });
        }

        public override void Tick(float dt)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].TimeLeft -= dt;
                if (_active[i].TimeLeft <= 0)
                {
                    _container?.Remove(_active[i].Element);
                    _active.RemoveAt(i);
                }
            }
        }

        private class ToastEntry
        {
            public VisualElement Element;
            public float TimeLeft;
        }
    }
}
