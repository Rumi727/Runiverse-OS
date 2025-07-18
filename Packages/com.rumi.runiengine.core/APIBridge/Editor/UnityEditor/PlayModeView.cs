#nullable enable
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class PlayModeView : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.PlayModeView");

        public static PlayModeView GetInstance(global::UnityEditor.EditorWindow? instance) => new PlayModeView(instance);

        protected PlayModeView(global::UnityEditor.EditorWindow? instance) => this.instance = instance;

        public global::UnityEditor.EditorWindow? instance { get; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
