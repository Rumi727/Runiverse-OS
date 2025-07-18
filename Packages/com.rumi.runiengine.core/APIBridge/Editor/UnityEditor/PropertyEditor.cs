#nullable enable
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class PropertyEditor : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.PropertyEditor");

        public static PropertyEditor GetInstance(global::UnityEditor.EditorWindow? instance) => new PropertyEditor(instance);

        protected PropertyEditor(global::UnityEditor.EditorWindow? instance) => this.instance = instance;

        public global::UnityEditor.EditorWindow? instance { get; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
