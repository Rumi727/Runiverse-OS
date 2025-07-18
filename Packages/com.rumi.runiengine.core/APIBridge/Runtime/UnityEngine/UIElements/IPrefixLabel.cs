#nullable enable
using System.Reflection;
using System;
using UnityEngine.UIElements;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public interface IPrefixLabel
    {
        public static Type type { get; } = AssemblyManager.UnityEngine_CoreModule.GetType("UnityEngine.UIElements.IPrefixLabel");

        public static IPrefixLabel GetInstance(object instance) => new PrefixLabel(Convert.ChangeType(instance, type));



        string label { get; }

        Label labelElement { get; }



        class PrefixLabel : IPrefixLabel
        {
            public PrefixLabel(object instance) => this.instance = instance;

            public object instance { get; }



            public string label
            {
                get
                {
                    f_label ??= type.GetField("label", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return (string)f_label.GetValue(instance);
                }
                set
                {
                    f_label ??= type.GetField("label", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    f_label.SetValue(instance, value);
                }
            }
            static FieldInfo? f_label;

            public Label labelElement
            {
                get
                {
                    f_labelElement ??= type.GetField("labelElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return (Label)f_labelElement.GetValue(instance);
                }
                set
                {
                    f_labelElement ??= type.GetField("labelElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    f_labelElement.SetValue(instance, value);
                }
            }
            static FieldInfo? f_labelElement;
        }
    }
}
