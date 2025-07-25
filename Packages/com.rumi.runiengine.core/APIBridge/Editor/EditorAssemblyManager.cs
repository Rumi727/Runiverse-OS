#nullable enable
using System;
using System.Linq;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge
{
    internal static class EditorAssemblyManager
    {
        public static Assembly[] assemblys => _assemblys ??= AppDomain.CurrentDomain.GetAssemblies();
        static Assembly[]? _assemblys;

        public static Assembly UnityEditor_CoreModule => _UnityEditor_CoreModule ??= assemblys.First(static x => x.GetName().Name == "UnityEditor.CoreModule");
        static Assembly? _UnityEditor_CoreModule;

        public static Assembly UnityEditor_UI => _UnityEditor_UI ??= assemblys.First(static x => x.GetName().Name == "UnityEditor.UI");
        static Assembly? _UnityEditor_UI;

        public static Assembly UnityEditor_UIElementsModule => _UnityEditor_UIElementsModule ??= assemblys.First(static x => x.GetName().Name == "UnityEditor.UIElementsModule");
        static Assembly? _UnityEditor_UIElementsModule;

        public static Assembly UnityEditor_UIBuilderModule => _UnityEditor_UIBuilderModule ??= assemblys.First(static x => x.GetName().Name == "UnityEditor.UIBuilderModule");
        static Assembly? _UnityEditor_UIBuilderModule;
    }
}
