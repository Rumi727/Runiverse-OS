#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class LocalizationManipulator : Manipulator
    {
        public LocalizationManipulator() { }
        public LocalizationManipulator(Action onLanguageChanged) => this.onLanguageChanged = onLanguageChanged;

        public event Action? onLanguageChanged;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<AttachToPanelEvent>(OnAttach);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
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