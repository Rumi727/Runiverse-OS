#nullable enable
using System.Reflection;
using System;
using UnityEngine.UIElements;

namespace RuniEngine.Editor.APIBridge.UnityEditor.UIElements
{
    public interface IUxmlAttributeConverter
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_UIElementsModule.GetType("UnityEditor.UIElements.IUxmlAttributeConverter");

        public static IUxmlAttributeConverter GetInstance(object instance) => new UxmlAttributeConverter(Convert.ChangeType(instance, type));



        object FromString(string value, CreationContext cc);

        string ToString(object value, VisualTreeAsset visualTreeAsset);



        class UxmlAttributeConverter : IUxmlAttributeConverter
        {
            public UxmlAttributeConverter(object instance) => this.instance = instance;

            public object instance { get; }



            static MethodInfo? m_FromString;
            static readonly object[] mp_FromString = new object[2];
            static readonly Type[] mpt_FromString = new Type[] { typeof(string), typeof(CreationContext) };
            public object FromString(string value, CreationContext cc)
            {
                m_FromString ??= type.GetMethod("FromString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, mpt_FromString, null);

                mp_FromString[0] = value;
                mp_FromString[1] = cc;

                return m_FromString.Invoke(instance, mp_FromString);
            }

            static MethodInfo? m_ToString;
            static readonly object[] mp_ToString = new object[2];
            static readonly Type[] mpt_ToString = new Type[] { typeof(object), typeof(VisualTreeAsset) };
            public string ToString(object value, VisualTreeAsset visualTreeAsset)
            {
                m_ToString ??= type.GetMethod("ToString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, mpt_ToString, null);

                mp_ToString[0] = value;
                mp_ToString[1] = visualTreeAsset;

                return (string)m_ToString.Invoke(instance, mp_ToString);
            }



            public override string ToString() => instance.ToString();
        }
    }
}
