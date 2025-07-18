#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace RuniEngine
{
    public static class TypeUtility
    {
        public static object? GetDefaultValue(this Type type)
        {
            if (!type.IsValueType)
                return null;

            return Activator.CreateInstance(type);
        }

        public static object GetDefaultValueNotNull(this Type type)
        {
            if (type == typeof(string))
                return string.Empty;

            return Activator.CreateInstance(type);
        }

        /// <summary>type != typeof(T) &amp;&amp; typeof(T).IsAssignableFrom(type)</summary>
        public static bool IsSubtypeOf<T>(this Type type) => type != typeof(T) && typeof(T).IsAssignableFrom(type);
        /// <summary>type != surclass &amp;&amp; surclass.IsAssignableFrom(type)</summary>
        public static bool IsSubtypeOf(this Type type, Type surclass) => type != surclass && surclass.IsAssignableFrom(type);

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType) => IsAssignableToGenericType(givenType, genericType, out _);
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType, out Type? resultType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                {
                    resultType = it;
                    return true;
                }
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                resultType = givenType;
                return true;
            }

            Type baseType = givenType.BaseType;
            if (baseType == null)
            {
                resultType = null;
                return false;
            }

            return IsAssignableToGenericType(baseType, genericType, out resultType);
        }

        public static IEnumerable<Type> GetHierarchy(this Type type)
        {
            if (type == typeof(object))
                yield break;

            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        /// <summary><see cref="IList"/> 인터페이스의 리스트 타입을 가져옵니다</summary>
        public static Type GetListType(this IList list) => GetListType(list.GetType()) ?? typeof(object);

        /// <summary><see cref="IList"/> 인터페이스의 리스트 타입을 가져옵니다</summary>
        public static Type? GetListType(this Type type)
        {
            if (type.IsAssignableToGenericType(typeof(IList<>), out Type? resultType))
                return resultType?.GetGenericArguments()[0] ?? typeof(object);
            else
                return null;
        }

        /// <summary><see cref="IList"/> 인터페이스의 리스트 타입을 가져옵니다</summary>
        public static (Type key, Type value) GetDictionaryType(this IDictionary list) => GetDictionaryType(list.GetType()) ?? (typeof(object), typeof(object));

        /// <summary><see cref="IDictionary"/> 인터페이스의 리스트 타입을 가져옵니다</summary>
        public static (Type key, Type value)? GetDictionaryType(this Type type)
        {
            if (type.IsAssignableToGenericType(typeof(IDictionary<,>), out Type? resultType))
            {
                Type[]? types = resultType?.GetGenericArguments();
                return (types?[0] ?? typeof(object), types?[1] ?? typeof(object));
            }
            else
                return null;
        }

        public static string GetTypeDisplayName(this Type type) => Unity.Properties.TypeUtility.GetTypeDisplayName(type);

        public static string SerializeToString(this Type type) => type.FullName + ", " + type.Assembly.GetName().Name;
        public static Type? DeserializeFromString(string typeName) => Type.GetType(typeName);
    }
}