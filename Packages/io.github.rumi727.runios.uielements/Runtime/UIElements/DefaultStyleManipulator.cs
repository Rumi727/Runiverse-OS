#nullable enable
using RuniOS.APIBridge.UnityEngine.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public sealed class DefaultStyleManipulator(ThemeStyleSheet baseStyle, ThemeStyleSheet? editorStyle = null) : EditorThemeAwareManipulator
    {
        public ThemeStyleSheet baseStyle { get; } = baseStyle;
        public ThemeStyleSheet? editorStyle { get; } = editorStyle;

#if UNITY_EDITOR
        bool editorStyleApplied;
#endif

        protected override void RegisterCallbacksOnTarget()
        {
            // Rank below every user theme, including the panel theme, regardless of where it is attached.
            StyleSheetBridge.__GetInstanceFrom(baseStyle).priority = StyleSheetPriorityBridge.Builtin;
            target.styleSheets.Add(baseStyle);

            base.RegisterCallbacksOnTarget();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            base.UnregisterCallbacksFromTarget();

#if UNITY_EDITOR
            if (editorStyle != null)
                target.styleSheets.Remove(editorStyle);
#endif
            target.styleSheets.Remove(baseStyle);
        }

        protected override void OnThemeChanged(EditorTheme editorTheme)
        {
            base.OnThemeChanged(editorTheme);

#if UNITY_EDITOR
            bool isEditorTheme = editorTheme != EditorTheme.none;
            if (editorStyle == null || isEditorTheme == editorStyleApplied)
                return;

            if (isEditorTheme)
                target.styleSheets.Add(editorStyle);
            else
                target.styleSheets.Remove(editorStyle);

            editorStyleApplied = isEditorTheme;
#endif
        }
    }
}