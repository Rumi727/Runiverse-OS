#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class InspectorWindow : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.InspectorWindow");

        public static InspectorWindow GetInstance(global::UnityEditor.EditorWindow? instance) => new InspectorWindow(instance);

        protected InspectorWindow(global::UnityEditor.EditorWindow? instance) => this.instance = instance;

        public global::UnityEditor.EditorWindow? instance { get; }



        static MethodInfo? m_RepaintAllInspectors;
        public static void RepaintAllInspectors()
        {
            m_RepaintAllInspectors ??= type.GetMethod("RepaintAllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
            m_RepaintAllInspectors.Invoke(null, null);
        }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
