#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEngine.ScriptableObject;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class GUIView : View, IWindowModel
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.GUIView");

        static readonly ConditionalWeakTable<BridgeTarget, GUIView> cached = new ConditionalWeakTable<BridgeTarget, GUIView>();
        [return: NotNullIfNotNull("instance")]
        public static new GUIView? GetInstance(BridgeTarget? instance)
        {
            if (instance == null)
                return null;
            
            if (!cached.TryGetValue(instance, out GUIView? element))
            {
                element = new GUIView(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        protected GUIView(BridgeTarget? instance) : base(instance) => this.instance = instance;

        public new BridgeTarget? instance { get; set; }


        public static GUIView? current
        {
            get
            {
                f_current ??= type.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                return GetInstance((BridgeTarget?)f_current!.GetValue(null));
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