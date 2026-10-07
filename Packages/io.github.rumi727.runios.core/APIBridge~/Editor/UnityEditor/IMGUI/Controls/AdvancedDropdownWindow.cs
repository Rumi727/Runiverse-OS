#nullable enable
using System;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEditor.EditorWindow;

namespace RuniOS.Editor.APIBridge.UnityEditor.IMGUI.Controls
{
    public class AdvancedDropdownWindow : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdownWindow");

        public static AdvancedDropdownWindow CreateInstance() => new AdvancedDropdownWindow((BridgeTarget?)Activator.CreateInstance(type));
        
        static readonly ConditionalWeakTable<BridgeTarget, AdvancedDropdownWindow> cached = new ConditionalWeakTable<BridgeTarget, AdvancedDropdownWindow>();
        public static AdvancedDropdownWindow GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out AdvancedDropdownWindow? element))
            {
                element = new AdvancedDropdownWindow(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        AdvancedDropdownWindow(BridgeTarget? instance) => this.instance = instance;

        public BridgeTarget? instance { get; set; }

        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
