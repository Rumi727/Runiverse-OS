#nullable enable
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(object), true)]
    public class ObjectPropertyBinder : PropertyBinder
    {
        public override object? Read(VisualElement element, SerializedProperty property, Type propertyType) => property.boxedValue;
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value) => property.boxedValue = value;
    }
}