#nullable enable
using RuniOS.Editor.Drawers;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(ISerializableNullable<>), true)]
    public class SerializableNullablePropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType)
        {
            object instance = Activator.CreateInstance(propertyType);
            
            Type? underlyingType = SerializableNullable.GetUnderlyingType(propertyType);
            if (underlyingType == null)
                return instance;
            
            PropertyBinder? valueBinder = FindBinder(underlyingType);
            if (valueBinder == null)
                return instance;
            
            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (field == null || toggle is not { boolValue: true })
                return instance;
            
            object? value = valueBinder.Read(element, field, underlyingType);
            if (value == null)
                return instance;
            
            AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfValue)?.SetValue(value, instance);
            return instance;
        }
        
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? nullable)
        {
            Type? underlyingType = SerializableNullable.GetUnderlyingType(propertyType);
            if (underlyingType == null)
                return;
            
            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (field == null || toggle == null)
                return;
            
            object? value = AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfValue)?.GetValue(nullable);
            toggle.boolValue = value != null;
            
            FindBinder(underlyingType)?.Write(element, field, underlyingType, value);
        }
    }
}