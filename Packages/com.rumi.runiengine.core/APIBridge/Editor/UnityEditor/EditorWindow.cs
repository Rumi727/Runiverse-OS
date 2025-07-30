#nullable enable
using RuniOS.APIBridge.UnityEngine;
using System;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class EditorWindow : ScriptableObject
    {
        public static new Type type { get; } = typeof(global::UnityEditor.EditorWindow);

        protected EditorWindow() { }
    }
}
