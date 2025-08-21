#nullable enable
using System;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEditor.EditorWindow;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public sealed class GameView : PlayModeView
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.GameView");

        static readonly ConditionalWeakTable<BridgeTarget, GameView> cached = new ConditionalWeakTable<BridgeTarget, GameView>();
        public static new GameView GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out GameView? element))
            {
                element = new GameView(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        GameView(BridgeTarget? instance) : base(instance) => this.instance = instance;

        public new BridgeTarget? instance { get; set; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
