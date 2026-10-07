#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// Editor-only. It performs no function at runtime.
    /// </summary>
    public class EditorThemeAwareManipulator : Manipulator
    {
        public EditorThemeAwareManipulator() { }
        public EditorThemeAwareManipulator(Action<bool>? onThemeChanged) => this.onThemeChanged = onThemeChanged;

        public event Action<bool>? onThemeChanged;

#if UNITY_EDITOR
        const string uiBuilderThemePropertyName = "__unity-ui-builder-linked-active-theme-stylesheet";

        IVisualElementScheduledItem? updateItem;
        object? lastBuilderTheme;
#endif

        protected override void RegisterCallbacksOnTarget()
        {
#if UNITY_EDITOR
            target.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            if (target.panel != null)
                UpdateTheme(true);
#endif
        }

        protected override void UnregisterCallbacksFromTarget()
        {
#if UNITY_EDITOR
            target.UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            Detach();
#endif
        }

#if UNITY_EDITOR
        void OnAttachedToPanel(AttachToPanelEvent evt) => UpdateTheme(true);

        void OnDetachedFromPanel(DetachFromPanelEvent evt) => Detach();

        void Detach()
        {
            updateItem?.Pause();
            lastBuilderTheme = null;
        }

        void UpdateTheme(bool initialize = false)
        {
            object? theme = null;
            bool hasBuilderThemeProperty = false;

            PropertyName propertyName = new(uiBuilderThemePropertyName);
            for (VisualElement? element = target; element != null; element = element.parent)
            {
                var bridge =
                    APIBridge.UnityEngine.UIElements.VisualElementBridge.__GetInstanceFrom(element);

                if (!bridge.HasProperty(propertyName))
                    continue;

                theme = bridge.GetProperty(propertyName);
                hasBuilderThemeProperty = true;
                break;
            }

            if (!initialize && hasBuilderThemeProperty && theme == lastBuilderTheme)
                return;

            lastBuilderTheme = theme;

            bool isEditorTheme = target.panel?.contextType == ContextType.Editor &&
                (!hasBuilderThemeProperty || (theme is StyleSheet && theme is not ThemeStyleSheet));

            OnThemeChanged(isEditorTheme);

            if (hasBuilderThemeProperty)
            {
                updateItem ??= target.schedule.Execute(() => UpdateTheme()).Every(100);
                updateItem.Resume();
            }
            else
                updateItem?.Pause();
        }
#endif

        protected virtual void OnThemeChanged(bool isEditorTheme) => onThemeChanged?.Invoke(isEditorTheme);
    }
}