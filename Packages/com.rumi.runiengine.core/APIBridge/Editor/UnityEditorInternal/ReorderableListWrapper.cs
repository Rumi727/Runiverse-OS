#nullable enable
using System;
using System.Reflection;
using UnityEditor;

namespace RuniEngine.Editor.APIBridge.UnityEditorInternal
{
    public sealed class ReorderableListWrapper
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditorInternal.ReorderableListWrapper");

        public static ReorderableListWrapper CreateInstance() => new ReorderableListWrapper(Activator.CreateInstance(type));
        public static ReorderableListWrapper GetInstance(object instance) => new ReorderableListWrapper(instance);

        ReorderableListWrapper(object instance) => this.instance = instance;

        public object instance { get; }



        static MethodInfo? m_GetPropertyIdentifier;
        static readonly object[] mp_GetPropertyIdentifier = new object[1];
        public static string GetPropertyIdentifier(SerializedProperty serializedProperty)
        {
            m_GetPropertyIdentifier ??= type.GetMethod("GetPropertyIdentifier", BindingFlags.Public | BindingFlags.Static);

            mp_GetPropertyIdentifier[0] = serializedProperty;
            return (string)m_GetPropertyIdentifier.Invoke(null, mp_GetPropertyIdentifier);
        }



        public override string ToString() => instance.ToString();
    }
}
