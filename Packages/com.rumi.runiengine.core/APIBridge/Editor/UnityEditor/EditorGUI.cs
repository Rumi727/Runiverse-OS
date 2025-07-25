#nullable enable
using System;
using System.Reflection;
using UnityEngine;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public sealed class EditorGUI
    {
        public static Type type { get; } = typeof(global::UnityEditor.EditorGUI);

        EditorGUI() { }



        public static int s_FoldoutHash
        {
            get
            {
                f_s_FoldoutHash ??= type.GetField("s_FoldoutHash", BindingFlags.NonPublic | BindingFlags.Static);
                return (int)f_s_FoldoutHash!.GetValue(null);
            }
        }
        static FieldInfo? f_s_FoldoutHash;
        


        static MethodInfo? m_HasKeyboardFocus;
        static readonly object[] mp_HasKeyboardFocus = new object[1];
        static readonly Type[] mpt_HasKeyboardFocus = new Type[] { typeof(int) };
        public static bool HasKeyboardFocus(int id)
        {
            m_HasKeyboardFocus ??= type.GetMethod("HasKeyboardFocus", BindingFlags.NonPublic | BindingFlags.Static, null, mpt_HasKeyboardFocus, null);
            mp_HasKeyboardFocus[0] = id;

            return (bool)m_HasKeyboardFocus!.Invoke(null, mp_HasKeyboardFocus);
        }

        static MethodInfo? m_MultiFieldPrefixLabel;
        static readonly object[] mp_MultiFieldPrefixLabel = new object[4];
        static readonly Type[] mpt_MultiFieldPrefixLabel = new Type[] { typeof(Rect), typeof(int), typeof(GUIContent), typeof(int) };
        public static Rect MultiFieldPrefixLabel(Rect totalPosition, int id, GUIContent label, int columns)
        {
            m_MultiFieldPrefixLabel ??= type.GetMethod("MultiFieldPrefixLabel", BindingFlags.NonPublic | BindingFlags.Static, null, mpt_MultiFieldPrefixLabel, null);

            mp_MultiFieldPrefixLabel[0] = totalPosition;
            mp_MultiFieldPrefixLabel[1] = id;
            mp_MultiFieldPrefixLabel[2] = label;
            mp_MultiFieldPrefixLabel[3] = columns;

            return (Rect)m_MultiFieldPrefixLabel!.Invoke(null, mp_MultiFieldPrefixLabel);
        }



        public class VUMeter
        {
            public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.EditorGUI+VUMeter");

            VUMeter() { }

            public struct SmoothingData
            {
                public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.EditorGUI+VUMeter+SmoothingData");

                public static SmoothingData CreateInstance() => new SmoothingData(Activator.CreateInstance(type));
                public static SmoothingData GetInstance(object instance) => new SmoothingData(instance);

                SmoothingData(object instance)
                {
                    this.instance = instance;

                    f_lastValue = null;
                    f_peakValue = null;
                    f_peakValueTime = null;
                }

                public object instance { get; }



                public float lastValue
                {
                    get
                    {
                        f_lastValue ??= type.GetField("lastValue", BindingFlags.Public | BindingFlags.Instance);
                        return (float)f_lastValue!.GetValue(instance);
                    }
                    set
                    {
                        f_lastValue ??= type.GetField("lastValue", BindingFlags.Public | BindingFlags.Instance);
                        f_lastValue!.SetValue(instance, value);
                    }
                }
                FieldInfo? f_lastValue;

                public float peakValue
                {
                    get
                    {
                        f_peakValue ??= type.GetField("peakValue", BindingFlags.Public | BindingFlags.Instance);
                        return (float)f_peakValue!.GetValue(instance);
                    }
                    set
                    {
                        f_peakValue ??= type.GetField("peakValue", BindingFlags.Public | BindingFlags.Instance);
                        f_peakValue!.SetValue(instance, value);
                    }
                }
                FieldInfo? f_peakValue;

                public float peakValueTime
                {
                    get
                    {
                        f_peakValueTime ??= type.GetField("peakValueTime", BindingFlags.Public | BindingFlags.Instance);
                        return (float)f_peakValueTime!.GetValue(instance);
                    }
                    set
                    {
                        f_peakValueTime ??= type.GetField("peakValueTime", BindingFlags.Public | BindingFlags.Instance);
                        f_peakValueTime!.SetValue(instance, value);
                    }
                }
                FieldInfo? f_peakValueTime;



                public override readonly string ToString() => instance.ToString();
            }
        }
    }
}
