#nullable enable
using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace RuniOS.Editor.APIBridge.UnityEditor
{
    public class ScriptAttributeUtility
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.ScriptAttributeUtility");
        
        protected ScriptAttributeUtility() { }
        
        static MethodInfo? m_GetFieldInfoFromProperty;
        static readonly object?[] mp_GetFieldInfoFromProperty = new object?[2];
        static readonly Type[] mpt_GetFieldInfoFromProperty = new Type[] { typeof(SerializedProperty), typeof(Type).MakeByRefType() };
        public static FieldInfo? GetFieldInfoFromProperty(SerializedProperty property, out Type? type)
        {
            m_GetFieldInfoFromProperty ??= ScriptAttributeUtility.type.GetMethod("GetFieldInfoFromProperty", BindingFlags.NonPublic | BindingFlags.Static, null, mpt_GetFieldInfoFromProperty, null);

            mp_GetFieldInfoFromProperty[0] = property;
            mp_GetFieldInfoFromProperty[1] = null;
            
            FieldInfo? result = (FieldInfo?)m_GetFieldInfoFromProperty!.Invoke(null, mp_GetFieldInfoFromProperty);
            
            type = (Type?)mp_GetFieldInfoFromProperty[1];
            return result;
        }
    }
}