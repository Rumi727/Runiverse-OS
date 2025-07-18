#nullable enable
using RuniEngine.APIBridge.UnityEngine;
using System;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public sealed class EditorGUIUtility : GUIUtility
    {
        public static new Type type { get; } = typeof(global::UnityEditor.EditorGUIUtility);

        EditorGUIUtility() { }

        public static int s_LastControlID
        {
            get
            {
                f_s_LastControlID ??= type.GetField("s_LastControlID", BindingFlags.NonPublic | BindingFlags.Static);
                return (int)f_s_LastControlID.GetValue(null);
            }
            set
            {
                f_s_LastControlID ??= type.GetField("s_LastControlID", BindingFlags.NonPublic | BindingFlags.Static);
                f_s_LastControlID.SetValue(null, value);
            }
        }
        static FieldInfo? f_s_LastControlID;

        public static float s_LabelWidth
        {
            get
            {
                f_s_LabelWidth ??= type.GetField("s_LabelWidth", BindingFlags.NonPublic | BindingFlags.Static);
                return (float)f_s_LabelWidth.GetValue(null);
            }
            set
            {
                f_s_LabelWidth ??= type.GetField("s_LabelWidth", BindingFlags.NonPublic | BindingFlags.Static);
                f_s_LabelWidth.SetValue(null, value);
            }
        }
        static FieldInfo? f_s_LabelWidth;

        public static float s_FieldWidth
        {
            get
            {
                f_s_FieldWidth ??= type.GetField("s_FieldWidth", BindingFlags.NonPublic | BindingFlags.Static);
                return (float)f_s_FieldWidth.GetValue(null);
            }
            set
            {
                f_s_FieldWidth ??= type.GetField("s_FieldWidth", BindingFlags.NonPublic | BindingFlags.Static);
                f_s_FieldWidth.SetValue(null, value);
            }
        }
        static FieldInfo? f_s_FieldWidth;

        public static float contextWidth
        {
            get
            {
                f_contextWidth ??= type.GetProperty("contextWidth", BindingFlags.NonPublic | BindingFlags.Static);
                return (float)f_contextWidth.GetValue(null);
            }
        }
        static PropertyInfo? f_contextWidth;
    }
}
