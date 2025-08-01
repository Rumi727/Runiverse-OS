#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;

using BridgeTarget = System.Object;

namespace RuniOS.Editor.APIBridge.UnityEditorInternal
{
    public sealed class ReorderableListWrapper
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditorInternal.ReorderableListWrapper");

        public static ReorderableListWrapper CreateInstance() => new ReorderableListWrapper(Activator.CreateInstance(type));
        
        static readonly ConditionalWeakTable<BridgeTarget, ReorderableListWrapper> cached = new ConditionalWeakTable<BridgeTarget, ReorderableListWrapper>();
        public static ReorderableListWrapper GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out ReorderableListWrapper? element))
            {
                element = new ReorderableListWrapper(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        ReorderableListWrapper(BridgeTarget instance) => this.instance = instance;

        public BridgeTarget instance { get; set; }



        static MethodInfo? m_GetPropertyIdentifier;
        static readonly object[] mp_GetPropertyIdentifier = new object[1];
        public static string GetPropertyIdentifier(SerializedProperty serializedProperty)
        {
            m_GetPropertyIdentifier ??= type.GetMethod("GetPropertyIdentifier", BindingFlags.Public | BindingFlags.Static);

            mp_GetPropertyIdentifier[0] = serializedProperty;
            return (string)m_GetPropertyIdentifier!.Invoke(null, mp_GetPropertyIdentifier);
        }



        public override string ToString() => instance.ToString();
    }
}
