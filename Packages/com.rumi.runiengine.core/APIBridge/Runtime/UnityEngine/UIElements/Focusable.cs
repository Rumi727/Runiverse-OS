#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class Focusable : CallbackEventHandler
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.Focusable);

        public static Focusable GetInstance(global::UnityEngine.UIElements.Focusable instance) => new Focusable(instance);

        protected Focusable(global::UnityEngine.UIElements.Focusable instance) : base(instance) => this.instance = instance;

        public new global::UnityEngine.UIElements.Focusable instance { get; set; }
    }
}
