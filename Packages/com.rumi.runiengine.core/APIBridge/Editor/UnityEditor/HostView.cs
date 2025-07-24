#nullable enable
using System;
using System.Reflection;
using UnityEngine;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class HostView : GUIView
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.HostView");

        public static new HostView GetInstance(UnityEngine.ScriptableObject? instance) => new HostView(instance);

        protected HostView(UnityEngine.ScriptableObject? instance) : base(instance) => this.instance = instance;

        public new UnityEngine.ScriptableObject? instance { get; }



        public global::UnityEditor.EditorWindow? actualView
        {
            get
            {
                f_actualView ??= type.GetProperty("actualView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (global::UnityEditor.EditorWindow?)f_actualView.GetValue(instance);
            }
            set
            {
                f_actualView ??= type.GetProperty("actualView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                f_actualView.SetValue(instance, value);
            }
        }
        static PropertyInfo? f_actualView;



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
