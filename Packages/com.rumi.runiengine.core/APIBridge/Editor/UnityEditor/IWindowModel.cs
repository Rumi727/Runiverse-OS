#nullable enable
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public interface IWindowModel
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.IWindowModel");
    }
}
