#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class BindableElement : VisualElement
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.BindableElement);

        protected BindableElement(global::UnityEngine.UIElements.BindableElement instance) : base(instance) { }
    }
}
