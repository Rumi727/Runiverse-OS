#nullable enable
using System;
using UnityEngine.UIElements;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class CallbackEventHandler : IEventHandler
    {
        public static Type type { get; } = typeof(global::UnityEngine.UIElements.CallbackEventHandler);

        public static CallbackEventHandler GetInstance(global::UnityEngine.UIElements.CallbackEventHandler instance) => new CallbackEventHandler(instance);

        protected CallbackEventHandler(global::UnityEngine.UIElements.CallbackEventHandler instance) => this.instance = instance;

        public global::UnityEngine.UIElements.CallbackEventHandler instance { get; set; }

        public void SendEvent(EventBase e) => ((IEventHandler)instance).HandleEvent(e);
        public void HandleEvent(EventBase evt) => ((IEventHandler)instance).HandleEvent(evt);
        public bool HasTrickleDownHandlers() => ((IEventHandler)instance).HasTrickleDownHandlers();
        public bool HasBubbleUpHandlers() => ((IEventHandler)instance).HasBubbleUpHandlers();
    }
}
