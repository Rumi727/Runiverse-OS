#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class LocalizationManipulator : Manipulator
    {
        public LocalizationManipulator() => reloadableManipulator = new ReloadableManipulator(OnLanguageChanged);
        public LocalizationManipulator(Action onLanguageChanged)
        {
            this.onLanguageChanged = onLanguageChanged;
            reloadableManipulator = new ReloadableManipulator(OnLanguageChanged);
        }

        public event Action? onLanguageChanged;

        readonly ReloadableManipulator reloadableManipulator;

        protected override void RegisterCallbacksOnTarget()
        {
            target.AddManipulator(reloadableManipulator);

            target.RegisterCallback<AttachToPanelEvent>(OnAttach);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.RemoveManipulator(reloadableManipulator);

            target.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

            //LocalizationUtility.languageChanged -= OnLanguageChanged;
        }

        protected virtual void OnAttach(AttachToPanelEvent evt)
        {
            //LocalizationUtility.languageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        protected virtual void OnDetach(DetachFromPanelEvent evt)
        {
            //LocalizationUtility.languageChanged -= OnLanguageChanged;
        }

        protected virtual void OnLanguageChanged() => onLanguageChanged?.Invoke();
    }
}