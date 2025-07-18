#nullable enable
using System;
using Unity.Android.Gradle;
using Unity.Properties;
using UnityEngine.UIElements;

namespace RuniEngine.UI
{
    [UxmlObject]
    public partial class HalfTheSizeBinding : CustomBinding
    {
        public HalfTheSizeBinding() => updateTrigger = BindingUpdateTrigger.OnSourceChanged;

        protected override void OnActivated(in BindingActivationContext context) => Update(context.targetElement, context.bindingId);

        protected override BindingResult Update(in BindingContext context) => Update(context.targetElement, context.bindingId);

        public BindingResult Update(VisualElement element, BindingId bindingId)
        {
            VisitReturnCode errorCode;

            if (float.IsNaN(element.resolvedStyle.width) || float.IsNaN(element.resolvedStyle.height))
                return new BindingResult(BindingStatus.Pending);

            if (element.resolvedStyle.width < element.resolvedStyle.height)
            {
                if (ConverterGroups.TrySetValueGlobal(ref element, bindingId, element.resolvedStyle.width / 2f, out errorCode))
                    return new BindingResult(BindingStatus.Success);
            }
            else
            {
                if (ConverterGroups.TrySetValueGlobal(ref element, bindingId, element.resolvedStyle.height / 2f, out errorCode))
                    return new BindingResult(BindingStatus.Success);
            }

            // Error handling
            var bindingTypeText = typeof(HalfTheSizeBinding).GetTypeDisplayName();
            var bindingIdText = $"{element.GetType().GetTypeDisplayName()}.{bindingId}";

            return errorCode switch
            {
                VisitReturnCode.InvalidPath => new BindingResult(BindingStatus.Failure, $"{bindingTypeText}: Binding id `{bindingIdText}` is either invalid or contains a `null` value."),
                VisitReturnCode.InvalidCast => new BindingResult(BindingStatus.Failure, $"{bindingTypeText}: Invalid conversion from `string` for binding id `{bindingIdText}`"),
                VisitReturnCode.AccessViolation => new BindingResult(BindingStatus.Failure, $"{bindingTypeText}: Trying set value for binding id `{bindingIdText}`, but it is read-only."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
