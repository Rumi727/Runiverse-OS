#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class BindableElement : VisualElement
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.BindableElement);

        public static BindableElement GetInstance(global::UnityEngine.UIElements.BindableElement instance) => new BindableElement(instance);

        protected BindableElement(global::UnityEngine.UIElements.BindableElement instance) : base(instance) => this.instance = instance;

        public new global::UnityEngine.UIElements.BindableElement instance { get; set; }
    }
}
