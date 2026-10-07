#nullable enable
using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

using BridgeTarget = UnityEngine.UIElements.CallbackEventHandler;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public class CallbackEventHandler : IEventHandler
    {
        public static Type type { get; } = typeof(BridgeTarget);

        static readonly ConditionalWeakTable<BridgeTarget, CallbackEventHandler> cached = new ConditionalWeakTable<BridgeTarget, CallbackEventHandler>();
        public static CallbackEventHandler GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out CallbackEventHandler? element))
            {
                element = new CallbackEventHandler(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected CallbackEventHandler(BridgeTarget instance) => this.instance = instance;

        public BridgeTarget instance { get; set; }

        public void SendEvent(EventBase e) => ((IEventHandler)instance).HandleEvent(e);
        public void HandleEvent(EventBase evt) => ((IEventHandler)instance).HandleEvent(evt);
        public bool HasTrickleDownHandlers() => ((IEventHandler)instance).HasTrickleDownHandlers();
        public bool HasBubbleUpHandlers() => ((IEventHandler)instance).HasBubbleUpHandlers();
    }
}
