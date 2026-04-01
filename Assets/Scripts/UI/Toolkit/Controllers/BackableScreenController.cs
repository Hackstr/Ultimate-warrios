using System;
using UnityEngine.UIElements;

namespace TacticalDuelist.UI.Toolkit
{
    /// <summary>
    /// Base controller for sub-screens with a back button (Settings, Heroes, etc.)
    /// </summary>
    public class BackableScreenController : UIScreenBase
    {
        public event Action OnBack;

        private Button _btnBack;

        protected override void QueryElements()
        {
            _btnBack = Root.Q<Button>("btn-back");
        }

        protected override void BindEvents()
        {
            _btnBack?.RegisterCallback<ClickEvent>(HandleBack);
        }

        protected override void UnbindEvents()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(HandleBack);
        }

        private void HandleBack(ClickEvent _) => OnBack?.Invoke();
    }
}
