#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class Focusable : CallbackEventHandler
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.Focusable);

        protected Focusable(global::UnityEngine.UIElements.Focusable instance) : base(instance) { }
    }
}
