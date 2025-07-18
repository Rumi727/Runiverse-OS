#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class VisualElement : Focusable
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.VisualElement);

        protected VisualElement(global::UnityEngine.UIElements.VisualElement instance) : base(instance) { }
    }
}
