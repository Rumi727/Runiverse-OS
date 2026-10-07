#nullable enable
using System;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public interface IWindowModel
    {
        static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.IWindowModel");
    }
}
