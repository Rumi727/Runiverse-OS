#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class GUIView : View, IWindowModel
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.GUIView");

        public static new GUIView GetInstance(UnityEngine.ScriptableObject? instance) => new GUIView(instance);

        protected GUIView(UnityEngine.ScriptableObject? instance) : base(instance) => this.instance = instance;

        public new UnityEngine.ScriptableObject? instance { get; }


        public static GUIView? current
        {
            get
            {
                f_current ??= type.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                return GetInstance((UnityEngine.ScriptableObject?)f_current!.GetValue(null));
            }
            set
            {
                f_current ??= type.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                f_current!.SetValue(null, value);
            }
        }
        static PropertyInfo? f_current;



        static MethodInfo? m_Repaint;
        public void Repaint()
        {
            m_Repaint ??= type.GetMethod("Repaint", BindingFlags.Public | BindingFlags.Instance);
            m_Repaint!.Invoke(instance, null);
        }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}