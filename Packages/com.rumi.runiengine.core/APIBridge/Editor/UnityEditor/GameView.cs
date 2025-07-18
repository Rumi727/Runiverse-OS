#nullable enable
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public sealed class GameView : PlayModeView
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.GameView");

        public static new GameView GetInstance(global::UnityEditor.EditorWindow? instance) => new GameView(instance);

        GameView(global::UnityEditor.EditorWindow? instance) : base(instance) => this.instance = instance;

        public new global::UnityEditor.EditorWindow? instance { get; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
