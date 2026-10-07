#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEditor.EditorWindow;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class PropertyEditor : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.PropertyEditor");

        static readonly ConditionalWeakTable<BridgeTarget, PropertyEditor> cached = new ConditionalWeakTable<BridgeTarget, PropertyEditor>();
        public static PropertyEditor GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out PropertyEditor? element))
            {
                element = new PropertyEditor(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected PropertyEditor(BridgeTarget? instance) => this.instance = instance;

        public global::UnityEditor.EditorWindow? instance { get; set; }



        static MethodInfo? m_RebuildContentsContainers;
        public void RebuildContentsContainers()
        {
            m_RebuildContentsContainers ??= type.GetMethod("RebuildContentsContainers", BindingFlags.NonPublic | BindingFlags.Static);
            m_RebuildContentsContainers!.Invoke(instance, null);
        }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
