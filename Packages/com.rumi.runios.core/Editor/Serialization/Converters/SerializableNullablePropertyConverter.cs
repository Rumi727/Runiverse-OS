#nullable enable
using RuniOS.Editor.Drawers;
using System;
using UnityEditor;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(ISerializableNullable<>), true)]
    public class SerializableNullablePropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            object instance = Activator.CreateInstance(propertyType);
            
            Type? underlyingType = SerializableNullable.GetUnderlyingType(propertyType);
            if (underlyingType == null)
                return instance;
            
            PropertyConverter? valueBinder = FindConverter(underlyingType);
            if (valueBinder == null)
                return instance;
            
            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (field == null || toggle is not { boolValue: true })
                return instance;
            
            object? value = valueBinder.Read(field, underlyingType);
            if (value == null)
                return instance;
            
            AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfValue)?.SetValue(value, instance);
            return instance;
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? nullable)
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
            
            FindConverter(underlyingType)?.Write(field, underlyingType, value);
            toggle.boolValue = hasValue;
        }
    }
}