#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuniEngine
{
    public static class ReflectionUtility
    {
        static ReflectionUtility() => Refresh();

        /// <summary>
        /// All loaded assemblys
        /// </summary>
        public static IReadOnlyList<Assembly> assemblys { get; private set; } = Array.Empty<Assembly>();

        /// <summary>
        /// All loaded types
        /// </summary>
        public static IReadOnlyList<Type> types { get; private set; } = Array.Empty<Type>();



        public static bool AttributeContains<T>(this MemberInfo element) where T : Attribute => element.AttributeContains(typeof(T));
        public static bool AttributeContains(this MemberInfo element, Type attribute) => Attribute.GetCustomAttributes(element, attribute).Length > 0;

        public static bool AttributeContains<T>(this Assembly element) where T : Attribute => element.AttributeContains(typeof(T));
        public static bool AttributeContains(this Assembly element, Type attribute) => Attribute.GetCustomAttributes(element, attribute).Length > 0;

        public static bool AttributeContains<T>(this ParameterInfo element) where T : Attribute => element.AttributeContains(typeof(T));
        public static bool AttributeContains(this ParameterInfo element, Type attribute) => Attribute.GetCustomAttributes(element, attribute).Length > 0;

        public static bool AttributeContains<T>(this Module element) where T : Attribute => element.AttributeContains(typeof(T));
        public static bool AttributeContains(this Module element, Type attribute) => element.GetCustomAttributes(attribute, false).Length > 0;

        public static bool IsAsyncMethod(this MethodBase methodBase) => methodBase.AttributeContains<AsyncStateMachineAttribute>();

        public static bool IsCompilerGenerated(this Type type) => type.AttributeContains<CompilerGeneratedAttribute>();
        public static bool IsCompilerGenerated(this MemberInfo memberInfo) => memberInfo.AttributeContains<CompilerGeneratedAttribute>();



        public static void Refresh()
        {
            assemblys = Array.AsReadOnly(AppDomain.CurrentDomain.GetAssemblies());

            List<Type> result = new List<Type>();
            for (int assemblysIndex = 0; assemblysIndex < assemblys.Count; assemblysIndex++)
            {
                Type[] types = assemblys[assemblysIndex].GetTypes();
                for (int typesIndex = 0; typesIndex < types.Length; typesIndex++)
                {
                    Type type = types[typesIndex];
                    result.Add(type);
                }
            }

            types = result.AsReadOnly();
        }

        public static void AttributeInvoke<T>() where T : Attribute
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            IReadOnlyList<Type> types = ReflectionUtility.types;
            for (int typesIndex = 0; typesIndex < types.Count; typesIndex++)
            {
                MethodInfo[] methodInfos = types[typesIndex].GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                for (int methodInfoIndex = 0; methodInfoIndex < methodInfos.Length; methodInfoIndex++)
                {
                    MethodInfo methodInfo = methodInfos[methodInfoIndex];
                    if (methodInfo.AttributeContains<T>() && methodInfo.GetParameters().Length <= 0)
                        methods.Add(methodInfo);
                }
            }

            for (int i = 0; i < methods.Count; i++)
            {
                try
                {
                    methods[i].Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
