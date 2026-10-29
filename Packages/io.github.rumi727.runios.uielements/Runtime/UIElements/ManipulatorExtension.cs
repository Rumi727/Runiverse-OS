#nullable enable
using RuniOS.Resource;
using RuniOS.Texts;
using RuniOS.Texts.Builders;
using RuniOS.Texts.Builders.RichTexts;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public static class ManipulatorExtension
    {
        public static void AddText<T>(this T element, Text text) where T : VisualElement, INotifyValueChanged<string> => element.AddText(text, element.SetValueWithoutNotify);

        public static void AddText(this VisualElement element, Text text, Action<string> setter) => element.AddText(text, RichTextBuilder.instance, setter);

        public static void AddText<TResult>(this VisualElement element, Text text, ITextBuilder<TResult> builder, Action<TResult> setter) => element.AddManipulator(new TextManipulator<TResult>(text, builder, setter));

        public static void AddText<T>(this T element, Identifier key) where T : VisualElement, INotifyValueChanged<string>
        {
            OnChanged();
            element.AddManipulator(new LocalizationManipulator(OnChanged));

            void OnChanged() => element.SetValueWithoutNotify(LocalizationUtility.GetTextOrKey(key));
        }

        public static void AddText<T>(this T element, Identifier key, params string[] args) where T : VisualElement, INotifyValueChanged<string>
        {
            OnChanged();
            element.AddManipulator(new LocalizationManipulator(OnChanged));

            void OnChanged() => element.SetValueWithoutNotify(string.Format(LocalizationUtility.GetTextOrKey(key), args));
        }

        public static void AddText<T>(this T element, Identifier key, params ReadOnlySpan<Text> args) where T : VisualElement, INotifyValueChanged<string> => element.AddText(Text.Local(key, args));
    }
}
