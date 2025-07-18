#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.Editor.APIBridge.UnityEditor.UIElements
{
    public static class UxmlAttributeConverter
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_UIElementsModule.GetType("UnityEditor.UIElements.UxmlAttributeConverter");



        static MethodInfo? m_TryGetConverter;
        static readonly object[] mp_TryGetConverter = new object[1];
        static readonly Type[] mpt_TryGetConverter = new Type[] { typeof(IUxmlAttributeConverter).MakeByRefType() };
        public static bool TryGetConverter<T>(out IUxmlAttributeConverter converter)
        {
            m_TryGetConverter ??= type.GetMethod("TryGetConverter", BindingFlags.Public | BindingFlags.Static, null, mpt_TryGetConverter, null);
            MethodInfo closedMethod = m_TryGetConverter.MakeGenericMethod(typeof(T));

            bool result = (bool)closedMethod.Invoke(null, mp_TryGetConverter);

            converter = IUxmlAttributeConverter.GetInstance(mp_TryGetConverter[0]);
            return result;
        }

        static MethodInfo? m2_TryGetConverter;
        static readonly object[] m2p_TryGetConverter = new object[2];
        static readonly Type[] m2pt_TryGetConverter = new Type[] { typeof(Type), IUxmlAttributeConverter.type.MakeByRefType() };
        public static bool TryGetConverter(Type type, out IUxmlAttributeConverter converter)
        {
            m2_TryGetConverter ??= UxmlAttributeConverter.type.GetMethod("TryGetConverter", BindingFlags.Public | BindingFlags.Static, null, m2pt_TryGetConverter, null);
            m2p_TryGetConverter[0] = type;
            
            bool result = (bool)m2_TryGetConverter.Invoke(null, m2p_TryGetConverter);

            converter = IUxmlAttributeConverter.GetInstance(m2p_TryGetConverter[1]);
            return result;
        }

        static MethodInfo? m_TryGetConverterType;
        static readonly object[] mp_TryGetConverterType = new object[2];
        static readonly Type[] mpt_TryGetConverterType = new Type[] { typeof(Type), typeof(Type).MakeByRefType() };
        public static bool TryGetConverterType(Type type, out Type converterType)
        {
            m_TryGetConverterType ??= UxmlAttributeConverter.type.GetMethod("TryGetConverterType", BindingFlags.Public | BindingFlags.Static, null, mpt_TryGetConverterType, null);
            mp_TryGetConverterType[0] = type;

            bool result = (bool)m_TryGetConverterType.Invoke(null, mp_TryGetConverterType);

            converterType = (Type)mp_TryGetConverterType[1];
            return result;
        }
    }
}
