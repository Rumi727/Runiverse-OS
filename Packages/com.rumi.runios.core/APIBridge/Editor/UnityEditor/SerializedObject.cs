#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEditor.SerializedObject;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class SerializedObject
    {
        public static Type type { get; } = typeof(BridgeTarget);

        static readonly ConditionalWeakTable<BridgeTarget, SerializedObject> cached = new ConditionalWeakTable<BridgeTarget, SerializedObject>();
        public static SerializedObject GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out SerializedObject? element))
            {
                element = new SerializedObject(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        SerializedObject(BridgeTarget instance) => this.instance = instance;

        public BridgeTarget instance { get; set; }



        public bool isValid
        {
            get
            {
                f_isValid ??= type.GetProperty("isValid", BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)f_isValid!.GetValue(instance);
            }
        }
        static PropertyInfo? f_isValid;

        public override string ToString() => instance.ToString();
    }
}
