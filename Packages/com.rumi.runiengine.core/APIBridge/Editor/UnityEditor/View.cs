#nullable enable
using RuniEngine.APIBridge.UnityEngine;
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class View : ScriptableObject
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.View");

        public static View GetInstance(UnityEngine.ScriptableObject? instance) => new View(instance);

        protected View(UnityEngine.ScriptableObject? instance) => this.instance = instance;

        public UnityEngine.ScriptableObject? instance { get; }



        public override string ToString() => instance != null ? instance.ToString() : "Null";
    }
}
