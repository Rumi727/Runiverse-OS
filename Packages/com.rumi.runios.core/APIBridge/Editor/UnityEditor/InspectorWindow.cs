#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEditor.EditorWindow;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class InspectorWindow : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.InspectorWindow");

        static readonly ConditionalWeakTable<BridgeTarget, InspectorWindow> cached = new ConditionalWeakTable<BridgeTarget, InspectorWindow>();
        public static InspectorWindow GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out InspectorWindow? element))
            {
                element = new InspectorWindow(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected InspectorWindow(BridgeTarget? instance) => this.instance = instance;

        public BridgeTarget? instance { get; set; }



        static MethodInfo? m_RepaintAllInspectors;
        public static void RepaintAllInspectors()
        {
            m_RepaintAllInspectors ??= type.GetMethod("RepaintAllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
            m_RepaintAllInspectors!.Invoke(null, null);
        }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
