#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEngine.ScriptableObject;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class HostView : GUIView
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.HostView");

        static readonly ConditionalWeakTable<BridgeTarget, HostView> cached = new ConditionalWeakTable<BridgeTarget, HostView>();
        public static new HostView GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out HostView? element))
            {
                element = new HostView(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected HostView(BridgeTarget? instance) : base(instance) => this.instance = instance;

        public new UnityEngine.ScriptableObject? instance { get; set; }



        public global::UnityEditor.EditorWindow? actualView
        {
            get
            {
                f_actualView ??= type.GetProperty("actualView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (global::UnityEditor.EditorWindow?)f_actualView!.GetValue(instance);
            }
            set
            {
                f_actualView ??= type.GetProperty("actualView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                f_actualView!.SetValue(instance, value);
            }
        }
        static PropertyInfo? f_actualView;



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
