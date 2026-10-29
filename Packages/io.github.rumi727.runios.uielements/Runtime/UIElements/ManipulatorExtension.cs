#nullable enable
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

        // 패널에 붙어 있는 동안에만 바인딩을 유지하여 텍스트가 요소를 붙잡지 않게 합니다.
        sealed class TextManipulator<TResult>(Text text, ITextBuilder<TResult> builder, Action<TResult> setter) : Manipulator
        {
            TextBinding<TResult>? binding;

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<AttachToPanelEvent>(OnAttach);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);

                if (target.panel != null)
                    binding = new TextBinding<TResult>(text, builder, setter);
                else
                    setter(builder.Build(text));
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<AttachToPanelEvent>(OnAttach);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

                binding?.Dispose();
                binding = null;
            }

            void OnAttach(AttachToPanelEvent evt) => binding = new TextBinding<TResult>(text, builder, setter);

            void OnDetach(DetachFromPanelEvent evt)
            {
                binding?.Dispose();
                binding = null;
            }
        }
    }
}
