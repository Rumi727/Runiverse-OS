#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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



        public static bool AttributeContains<T>(this MemberInfo element, bool inherit = true) where T : Attribute => element.AttributeContains(typeof(T), inherit);
        public static bool AttributeContains(this MemberInfo element, Type attribute, bool inherit = true) => Attribute.GetCustomAttributes(element, attribute, inherit).Length > 0;

        public static bool AttributeContains<T>(this Assembly element, bool inherit = true) where T : Attribute => element.AttributeContains(typeof(T), inherit);
        public static bool AttributeContains(this Assembly element, Type attribute, bool inherit = true) => Attribute.GetCustomAttributes(element, attribute, inherit).Length > 0;

        public static bool AttributeContains<T>(this ParameterInfo element, bool inherit = true) where T : Attribute => element.AttributeContains(typeof(T), inherit);
        public static bool AttributeContains(this ParameterInfo element, Type attribute, bool inherit = true) => Attribute.GetCustomAttributes(element, attribute, inherit).Length > 0;

        public static bool AttributeContains<T>(this Module element, bool inherit = true) where T : Attribute => element.AttributeContains(typeof(T), inherit);
        public static bool AttributeContains(this Module element, Type attribute, bool inherit = true) => element.GetCustomAttributes(attribute, inherit).Length > 0;

        public static bool IsAsyncMethod(this MethodBase methodBase) => methodBase.AttributeContains<AsyncStateMachineAttribute>();

        public static bool IsCompilerGenerated(this Type type) => type.AttributeContains<CompilerGeneratedAttribute>();
        public static bool IsCompilerGenerated(this MemberInfo memberInfo) => memberInfo.AttributeContains<CompilerGeneratedAttribute>();



        public static void Refresh()
        {
            assemblys = Array.AsReadOnly(AppDomain.CurrentDomain.GetAssemblies());
            types = assemblys.SelectMany(static x => x.GetTypes()).ToArray().AsReadOnly();
        }

        public static void AttributeInvoke<T>() where T : Attribute
        {
            var methods = types.SelectMany
            (
                static x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            )
            .Where
            (
                static x =>
                    x.AttributeContains<T>() &&
                    x.GetParameters().IsEmpty()
            );

            foreach (var item in methods)
            {
                try
                {
                    item.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        public static MethodInfo GetMethodInfo(Delegate method) => method.Method;
        
        public static bool HasDefaultConstructor(this Type t, bool nonPublic = false)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
                flags |= BindingFlags.NonPublic; 
            
            return t.IsValueType || t.GetConstructor(flags, null, Type.EmptyTypes, null) != null;
        }
    }
}
