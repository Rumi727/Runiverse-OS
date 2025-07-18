#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public static class EditorSettings
    {
        public static Type type { get; } = typeof(global::UnityEditor.EditorSettings);

        public static bool inspectorUseIMGUIDefaultInspector
        {
            get
            {
                f_inspectorUseIMGUIDefaultInspector ??= type.GetProperty("inspectorUseIMGUIDefaultInspector", BindingFlags.NonPublic | BindingFlags.Static);
                return (bool)f_inspectorUseIMGUIDefaultInspector.GetValue(null);
            }
            set
            {
                f_inspectorUseIMGUIDefaultInspector ??= type.GetProperty("inspectorUseIMGUIDefaultInspector", BindingFlags.NonPublic | BindingFlags.Static);
                f_inspectorUseIMGUIDefaultInspector.SetValue(null, value);
            }
        }
        static PropertyInfo? f_inspectorUseIMGUIDefaultInspector;
    }
}
