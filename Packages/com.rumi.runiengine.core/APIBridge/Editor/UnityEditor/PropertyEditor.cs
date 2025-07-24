#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class PropertyEditor : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.PropertyEditor");

        public static PropertyEditor GetInstance(global::UnityEditor.EditorWindow? instance) => new PropertyEditor(instance);

        protected PropertyEditor(global::UnityEditor.EditorWindow? instance) => this.instance = instance;

        public global::UnityEditor.EditorWindow? instance { get; }



        static MethodInfo? m_RebuildContentsContainers;
        public void RebuildContentsContainers()
        {
            m_RebuildContentsContainers ??= type.GetMethod("RebuildContentsContainers", BindingFlags.NonPublic | BindingFlags.Static);
            m_RebuildContentsContainers.Invoke(instance, null);
        }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
