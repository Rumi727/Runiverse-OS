#nullable enable
using RuniOS.APIBridge.UnityEngine;
using System;
using System.Runtime.CompilerServices;
using BridgeTarget = UnityEngine.ScriptableObject;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class View : ScriptableObject
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.View");

        static readonly ConditionalWeakTable<BridgeTarget, View> cached = new ConditionalWeakTable<BridgeTarget, View>();
        public static View GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out View? element))
            {
                element = new View(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected View(BridgeTarget? instance) => this.instance = instance;

        public BridgeTarget? instance { get; set; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
