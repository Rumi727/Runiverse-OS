#nullable enable
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

using BridgeTarget = System.Object;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public interface IPrefixLabel
    {
        static Type type { get; } = AssemblyManager.UnityEngine_CoreModule.GetType("UnityEngine.UIElements.IPrefixLabel");

        private static readonly ConditionalWeakTable<BridgeTarget, PrefixLabel> cached = new ConditionalWeakTable<BridgeTarget, PrefixLabel>();
        static IPrefixLabel GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out PrefixLabel? element))
            {
                element = new PrefixLabel(Convert.ChangeType(instance, type));
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }



        string label { get; }

        Label labelElement { get; }



        class PrefixLabel : IPrefixLabel
        {
            public PrefixLabel(object instance) => this.instance = instance;

            public object instance { get; set; }



            public string label
            {
                get
                {
                    f_label ??= type.GetField("label", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return (string)f_label!.GetValue(instance);
                }
                set
                {
                    f_label ??= type.GetField("label", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    f_label!.SetValue(instance, value);
                }
            }
            static FieldInfo? f_label;

            public Label labelElement
            {
                get
                {
                    f_labelElement ??= type.GetField("labelElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return (Label)f_labelElement!.GetValue(instance);
                }
                set
                {
                    f_labelElement ??= type.GetField("labelElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    f_labelElement!.SetValue(instance, value);
                }
            }
            static FieldInfo? f_labelElement;
        }
    }
}
