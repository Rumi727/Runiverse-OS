#nullable enable
using System;
using System.Diagnostics;
using System.Reflection;

using UniObject = UnityEngine.Object;

namespace RuniEngine.APIBridge.UnityEngine
{
    public class DrivenPropertyManager
    {
        public static Type type { get; } = AssemblyManager.UnityEngine_CoreModule.GetType("UnityEngine.DrivenPropertyManager");

        protected DrivenPropertyManager() { }



        static MethodInfo? m_RegisterProperty;
        static readonly object[] mp_RegisterProperty = new object[3];
        static readonly Type[] mpt_RegisterProperty = new Type[] { typeof(UniObject), typeof(UniObject), typeof(string) };
        [Conditional("UNITY_EDITOR")]
        public static void RegisterProperty(UniObject driver, UniObject target, string propertyPath)
        {
            m_RegisterProperty ??= type.GetMethod("RegisterProperty", BindingFlags.Public | BindingFlags.Static, null, mpt_RegisterProperty, null);
            
            mp_RegisterProperty[0] = driver;
            mp_RegisterProperty[1] = target;
            mp_RegisterProperty[2] = propertyPath;

            m_RegisterProperty.Invoke(null, mp_RegisterProperty);
        }

        static MethodInfo? m_UnregisterProperty;
        static readonly object[] mp_UnregisterProperty = new object[3];
        static readonly Type[] mpt_UnregisterProperty = new Type[] { typeof(UniObject), typeof(UniObject), typeof(string) };
        [Conditional("UNITY_EDITOR")]
        public static void UnregisterProperty(UniObject driver, UniObject target, string propertyPath)
        {
            m_UnregisterProperty ??= type.GetMethod("UnregisterProperty", BindingFlags.Public | BindingFlags.Static, null, mpt_UnregisterProperty, null);

            mp_UnregisterProperty[0] = driver;
            mp_UnregisterProperty[1] = target;
            mp_UnregisterProperty[2] = propertyPath;

            m_UnregisterProperty.Invoke(null, mp_UnregisterProperty);
        }
    }
}
