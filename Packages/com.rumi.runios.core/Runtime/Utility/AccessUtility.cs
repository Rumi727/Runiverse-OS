#nullable enable
// https://github.com/pardeike/Harmony/blob/master/Harmony/Tools/AccessTools.cs/ 소스 코드의 일부분을 가져왔습니다!
using System;
using System.Reflection;

namespace RuniOS
{
    public static class AccessUtility
    {
        /// <summary>Shortcut for <see cref="BindingFlags"/> to simplify the use of reflections and make it work for any access level</summary>
        public const BindingFlags all = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.GetField
            | BindingFlags.SetField
            | BindingFlags.GetProperty
            | BindingFlags.SetProperty;
        
        /// <summary>Shortcut for <see cref="BindingFlags"/> to simplify the use of reflections and make it work for any access level but only within the current type</summary>
        public const BindingFlags allDeclared = all | BindingFlags.DeclaredOnly;
        
        /// <summary>Applies a function going up the type hierarchy and stops at the first non-<c>null</c> result</summary>
        /// <typeparam name="T">Result type of func()</typeparam>
        /// <param name="type">The class/type to start with</param>
        /// <param name="func">The evaluation function returning T</param>
        /// <returns>The first non-<c>null</c> result, or <c>null</c> if no match</returns>
        /// <remarks>
        /// The type hierarchy of a class or value type (including struct) does NOT include implemented interfaces,
        /// and the type hierarchy of an interface is only itself (regardless of whether that interface implements other interfaces).
        /// The top-most type in the type hierarchy of all non-interface types (including value types) is <see cref="object"/>.
        /// </remarks>
        public static T? FindIncludingBaseTypes<T>(Type? type, Func<Type, T?> func) where T : class
        {
            for (; type != null; type = type.BaseType)
            {
                T? result = func(type);
                if (result != null)
                    return result;
            }

            return null;
        }
        
        /// <summary>Gets the reflection information for a directly declared field</summary>
        /// <param name="type">The class/type where the field is defined</param>
        /// <param name="name">The name of the field</param>
        /// <returns>A field or null when type/name is null or when the field cannot be found</returns>
        public static FieldInfo? DeclaredField(Type type, string name)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("DeclaredField: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("DeclaredField: name is null/empty");
                return null;
            }
            
            FieldInfo? field = type.GetField(name, allDeclared);
            if (field is null)
                Debug.Log($"DeclaredField: Could not find field for type {type} and name {name}");
            
            return field;
        }
        
        /// <summary>Gets the reflection information for a field by searching the type and all its super types</summary>
        /// <param name="type">The class/type where the field is defined</param>
        /// <param name="name">The name of the field (case sensitive)</param>
        /// <returns>A field or null when type/name is null or when the field cannot be found</returns>
        public static FieldInfo? Field(Type type, string name)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("Field: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("Field: name is null/empty");
                return null;
            }
            
            FieldInfo? field = FindIncludingBaseTypes(type, t => t.GetField(name, all));
            if (field is null)
                Debug.Log("Field: Could not find field for type {type} and name {name}");
            
            return field;
        }
        
        /// <summary>Gets the reflection information for a directly declared property</summary>
        /// <param name="type">The class/type where the property is declared</param>
        /// <param name="name">The name of the property (case sensitive)</param>
        /// <returns>A property or null when type/name is null or when the property cannot be found</returns>
        public static PropertyInfo? DeclaredProperty(Type type, string name)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("DeclaredProperty: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("DeclaredProperty: name is null/empty");
                return null;
            }
            
            PropertyInfo? property = type.GetProperty(name, allDeclared);
            if (property is null)
                Debug.Log("DeclaredProperty: Could not find property for type {type} and name {name}");
            
            return property;
        }
        
        /// <summary>Gets the reflection information for the getter method of a directly declared property</summary>
        /// <param name="type">The class/type where the property is declared</param>
        /// <param name="name">The name of the property (case sensitive)</param>
        /// <returns>A method or null when type/name is null or when the property cannot be found</returns>
        public static MethodInfo? DeclaredPropertyGetter(Type type, string name) => DeclaredProperty(type, name)?.GetGetMethod(true);
        
        /// <summary>Gets the reflection information for the setter method of a directly declared property</summary>
        /// <param name="type">The class/type where the property is declared</param>
        /// <param name="name">The name of the property (case sensitive)</param>
        /// <returns>A method or null when type/name is null or when the property cannot be found</returns>
        public static MethodInfo? DeclaredPropertySetter(Type type, string name) => DeclaredProperty(type, name)?.GetSetMethod(true);
        
        /// <summary>Gets the reflection information for a property by searching the type and all its super types</summary>
        /// <param name="type">The class/type</param>
        /// <param name="name">The name</param>
        /// <returns>A property or null when type/name is null or when the property cannot be found</returns>
        public static PropertyInfo? Property(Type type, string name)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("Property: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("Property: name is null/empty");
                return null;
            }
            
            PropertyInfo? property = FindIncludingBaseTypes(type, t => t.GetProperty(name, all));
            if (property is null)
                Debug.Log("Property: Could not find property for type {type} and name {name}");
            
            return property;
        }
        
        /// <summary>Gets the reflection information for the getter method of a property by searching the type and all its super types</summary>
        /// <param name="type">The class/type</param>
        /// <param name="name">The name</param>
        /// <returns>A method or null when type/name is null or when the property cannot be found</returns>
        public static MethodInfo? PropertyGetter(Type type, string name) => Property(type, name)?.GetGetMethod(true);
        
        /// <summary>Gets the reflection information for the setter method of a property by searching the type and all its super types</summary>
        /// <param name="type">The class/type</param>
        /// <param name="name">The name</param>
        /// <returns>A method or null when type/name is null or when the property cannot be found</returns>
        public static MethodInfo? PropertySetter(Type type, string name) => Property(type, name)?.GetSetMethod(true);
        
        /// <summary>Gets the reflection information for a directly declared event</summary>
        /// <param name="type">The class/type where the event is declared</param>
        /// <param name="name">The name of the event (case sensitive)</param>
        /// <returns>An event or null when type/name is null or when the event cannot be found</returns>
        public static EventInfo? DeclaredEvent(Type type, string name)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("DeclaredEvent: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("DeclaredEvent: name is null/empty");
                return null;
            }
            
            EventInfo? eventInfo = type.GetEvent(name, allDeclared);
            if (eventInfo is null)
                Debug.Log("DeclaredEvent: Could not find event for type {type} and name {name}");
            
            return eventInfo;
        }
        
        /// <summary>Gets the reflection information for an event by searching the type and all its super types</summary>
        /// <param name="type">The class/type where the event is declared</param>
        /// <param name="name">The name of the event (case sensitive)</param>
        /// <returns>An event or null when type/name is null or when the event cannot be found</returns>
        ///
        public static EventInfo? Event(Type type, string name)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("Event: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("Event: name is null/empty");
                return null;
            }
            
            EventInfo? eventInfo = FindIncludingBaseTypes(type, t => t.GetEvent(name, all));
            if (eventInfo is null)
                Debug.Log("Event: Could not find event for type {type} and name {name}");
            
            return eventInfo;
        }
        
        /// <summary>Gets the reflection information for the add method of a directly declared event</summary>
        /// <param name="type">The class/type where the event is declared</param>
        /// <param name="name">The name of the event (case sensitive)</param>
        /// <returns>A method or null when type/name is null or when the event cannot be found</returns>
        public static MethodInfo? DeclaredEventAdder(Type type, string name) => DeclaredEvent(type, name)?.GetAddMethod(true);
        
        /// <summary>Gets the reflection information for the add method of an event by searching the type and all its super types</summary>
        /// <param name="type">The class/type where the event is declared</param>
        /// <param name="name">The name of the event (case sensitive)</param>
        /// <returns>A method or null when type/name is null or when the event cannot be found</returns>
        public static MethodInfo? EventAdder(Type type, string name) => Event(type, name)?.GetAddMethod(true);
        
        /// <summary>Gets the reflection information for the remove method of a directly declared event</summary>
        /// <param name="type">The class/type where the event is declared</param>
        /// <param name="name">The name of the event (case sensitive)</param>
        /// <returns>A method or null when type/name is null or when the event cannot be found</returns>
        public static MethodInfo? DeclaredEventRemover(Type type, string name) => DeclaredEvent(type, name)?.GetRemoveMethod(true);
        
        /// <summary>Gets the reflection information for the remove method of an event by searching the type and all its super types</summary>
        /// <param name="type">The class/type where the event is declared</param>
        /// <param name="name">The name of the event (case sensitive)</param>
        /// <returns>A method or null when type/name is null or when the event cannot be found</returns>
        public static MethodInfo? EventRemover(Type type, string name) => Event(type, name)?.GetRemoveMethod(true);
        
        /// <summary>Gets the reflection information for a directly declared method</summary>
        /// <param name="type">The class/type where the method is declared</param>
        /// <param name="name">The name of the method (case sensitive)</param>
        /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
        /// <param name="generics">Optional list of types that define the generic version of the method</param>
        /// <returns>A method or null when type/name is null or when the method cannot be found</returns>
        public static MethodInfo? DeclaredMethod(Type type, string name, Type[]? parameters = null, Type[]? generics = null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("DeclaredMethod: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("DeclaredMethod: name is null/empty");
                return null;
            }
            
            MethodInfo? result;
            ParameterModifier[] modifiers = Array.Empty<ParameterModifier>();
            if (parameters is null)
                result = type.GetMethod(name, allDeclared);
            else
                result = type.GetMethod(name, allDeclared, null, parameters, modifiers);

            if (result is null)
            {
                Debug.Log("DeclaredMethod: Could not find method for type {type} and name {name} and parameters {parameters?.Description()}");
                return null;
            }
            else if (generics is not null)
                result = result.MakeGenericMethod(generics);
            
            return result;
        }
        
        /// <summary>Gets the reflection information for a method by searching the type and all its super types</summary>
        /// <param name="type">The class/type where the method is declared</param>
        /// <param name="name">The name of the method (case sensitive)</param>
        /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
        /// <param name="generics">Optional list of types that define the generic version of the method</param>
        /// <returns>A method or null when type/name is null or when the method cannot be found</returns>
        public static MethodInfo? Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type is null)
            {
                Debug.Log("Method: type is null");
                return null;
            }
            else if (string.IsNullOrEmpty(name))
            {
                Debug.Log("Method: name is null/empty");
                return null;
            }
            
            MethodInfo? result;
            ParameterModifier[] modifiers = Array.Empty<ParameterModifier>();
            if (parameters is null)
            {
                try
                {
                    result = FindIncludingBaseTypes(type, t => t.GetMethod(name, all));
                }
                catch (AmbiguousMatchException ex)
                {
                    result = FindIncludingBaseTypes(type, t => t.GetMethod(name, all, null, Array.Empty<Type>(), modifiers));
                    if (result is null)
                        throw new AmbiguousMatchException($"Ambiguous match in method for {type}:{name}", ex);
                }
            }
            else
                result = FindIncludingBaseTypes(type, t => t.GetMethod(name, all, null, parameters, modifiers));

            if (result is null)
            {
                Debug.Log("Method: Could not find method for type {type} and name {name} and parameters {parameters?.Description()}");
                return null;
            }
            else if (generics is not null)
                result = result.MakeGenericMethod(generics);
            
            return result;
        }
    }
}