#nullable enable
using System;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEngine.UIElements.Focusable;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public class Focusable : CallbackEventHandler
    {
        public static new Type type { get; } = typeof(BridgeTarget);

        static readonly ConditionalWeakTable<BridgeTarget, Focusable> cached = new ConditionalWeakTable<BridgeTarget, Focusable>();
        public static Focusable GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out Focusable? element))
            {
                element = new Focusable(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected Focusable(BridgeTarget instance) : base(instance) => this.instance = instance;

        public new BridgeTarget instance { get; set; }
    }
}
