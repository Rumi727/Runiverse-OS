#nullable enable
using RuniOS.Texts;
using RuniOS.Texts.Builders;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    // 패널에 붙어 있는 동안에만 바인딩을 유지하여 텍스트가 요소를 붙잡지 않게 합니다.
    public sealed class TextManipulator<TResult>(Text text, ITextBuilder<TResult> builder, Action<TResult> setter) : Manipulator
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
