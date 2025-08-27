#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Editor.Drawers;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(ISerializableKeyValuePair<,>), true)]
    public class SerializableKeyValuePairPropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType)
        {
            object instance = Activator.CreateInstance(propertyType);
            (Type? keyType, Type? valueType) = SerializableKeyValuePair.GetUnderlyingType(propertyType);
            
            (SerializedProperty? keyProperty, SerializedProperty? valueProperty) = SerializableKeyValuePairPropertyDrawer.GetChildProperty(property);
            if (keyType != null && keyProperty != null)
            {
                PropertyBinder? keyBinder = FindBinder(keyType);
                if (keyBinder != null)
                {
                    object? key = keyBinder.Read(element, keyProperty, keyType);
                    AccessUtility.DeclaredProperty(propertyType, SerializableKeyValuePair.nameOfKey)?.SetValue(key, instance);
                }
            }

            if (valueType != null && valueProperty != null)
            {
                PropertyBinder? valueBinder = FindBinder(valueType);
                if (valueBinder != null)
                {
                    object? value = valueBinder.Read(element, valueProperty, valueType);
                    AccessUtility.DeclaredProperty(propertyType, SerializableKeyValuePair.nameOfValue)?.SetValue(value, instance);
                }
            }

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

            object? value = null;
            bool hasValue = (bool)(AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfHasValue)?.GetValue(nullable) ?? false);
            if (hasValue)
                value = AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfValue)?.GetValue(nullable);
            
            FindBinder(underlyingType)?.Write(element, field, underlyingType, value);
            toggle.boolValue = hasValue;
        }
    }
}