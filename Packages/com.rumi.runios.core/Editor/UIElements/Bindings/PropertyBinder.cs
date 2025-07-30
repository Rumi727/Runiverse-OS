#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    public abstract class PropertyBinder
    {
        public static IReadOnlyList<(Type type, CustomPropertyBinderAttribute attribute)> propertyBinderTypes { get; } = ReflectionUtility.types.Where
        (
            static x =>
            x.AttributeContains(typeof(CustomPropertyBinderAttribute)) &&
            x.IsSubclassOf(typeof(PropertyBinder)) &&
            x.HasDefaultConstructor()
        )
        .Select(static x => (x, x.GetCustomAttribute<CustomPropertyBinderAttribute>()))
        .OrderByDescending
        (
            static x => x.Item2.targetType
                .GetHierarchy()
                .Count()
        ).ToArray().AsReadOnly();
        
        
        
        public abstract object? Read(VisualElement element, SerializedProperty property, Type propertyType);
        public abstract void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value);

        public virtual bool Comparer(VisualElement element, SerializedProperty property, Type propertyType, object? current, object? valueToCompare) => Equals(current, valueToCompare);
    }
}