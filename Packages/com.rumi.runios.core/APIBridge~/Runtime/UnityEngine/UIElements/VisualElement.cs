#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

using BridgeTarget = UnityEngine.UIElements.VisualElement;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public class VisualElement : Focusable
    {
        public static new Type type { get; } = typeof(BridgeTarget);

        static readonly ConditionalWeakTable<BridgeTarget, VisualElement> cached = new ConditionalWeakTable<BridgeTarget, VisualElement>();
        public static VisualElement GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out VisualElement? element))
            {
                element = new VisualElement(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected VisualElement(BridgeTarget instance) : base(instance) => this.instance = instance;

        public new BridgeTarget instance { get; set; }

        

        static MethodInfo? m_IncrementVersion;
        static readonly object[] mp_IncrementVersion = new object[1];
        static readonly Type[] mpt_IncrementVersion = new Type[] { typeof(VersionChangeType) };
        public void IncrementVersion(VersionChangeType changeType)
        {
            m_IncrementVersion ??= type.GetMethod("IncrementVersion", BindingFlags.NonPublic | BindingFlags.Instance, null, mpt_IncrementVersion, null);

            mp_IncrementVersion[0] = changeType;
            m_IncrementVersion!.Invoke(instance, mp_IncrementVersion);
        }
    }
}
