#nullable enable
using System;
using System.Runtime.CompilerServices;
using BridgeTarget = UnityEditor.EditorWindow;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class PlayModeView : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.PlayModeView");

        static readonly ConditionalWeakTable<BridgeTarget, PlayModeView> cached = new ConditionalWeakTable<BridgeTarget, PlayModeView>();
        public static PlayModeView GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out PlayModeView? element))
            {
                element = new PlayModeView(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected PlayModeView(BridgeTarget? instance) => this.instance = instance;

        public BridgeTarget? instance { get; set; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
