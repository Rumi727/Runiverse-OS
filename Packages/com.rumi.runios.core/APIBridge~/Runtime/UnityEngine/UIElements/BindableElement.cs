#nullable enable
using System;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEngine.UIElements.BindableElement;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public class BindableElement : VisualElement
    {
        public static new Type type { get; } = typeof(BridgeTarget);

        static readonly ConditionalWeakTable<BridgeTarget, BindableElement> cached = new ConditionalWeakTable<BridgeTarget, BindableElement>();
        public static BindableElement GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out BindableElement? element))
            {
                element = new BindableElement(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected BindableElement(BridgeTarget instance) : base(instance) => this.instance = instance;

        public new BridgeTarget instance { get; set; }
    }
}
