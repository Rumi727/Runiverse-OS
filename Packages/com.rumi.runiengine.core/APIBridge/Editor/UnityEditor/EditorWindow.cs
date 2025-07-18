#nullable enable
using RuniEngine.APIBridge.UnityEngine;
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class EditorWindow : ScriptableObject
    {
        public static new Type type { get; } = typeof(global::UnityEditor.EditorWindow);

        protected EditorWindow() { }
    }
}
