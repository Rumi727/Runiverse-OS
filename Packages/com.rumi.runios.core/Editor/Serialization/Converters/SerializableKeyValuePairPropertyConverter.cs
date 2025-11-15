#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Editor.IMGUI.Drawers;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(ISerializableKeyValuePair<,>), true)]
    public class SerializableKeyValuePairPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            (Type? keyType, Type? valueType) = SerializableKeyValuePair.GetUnderlyingType(propertyType);

            object? key = null;
            (SerializedProperty? keyProperty, SerializedProperty? valueProperty) = SerializableKeyValuePairPropertyDrawer.GetChildProperty(property);
            if (keyType != null && keyProperty != null)
            {
                PropertyConverter? keyBinder = FindConverter(keyType);
                if (keyBinder != null)
                    key = keyBinder.Read(keyProperty, keyType);
            }

            object? value = null;
            if (valueType != null && valueProperty != null)
            {
                PropertyConverter? valueBinder = FindConverter(valueType);
                if (valueBinder != null)
                    value = valueBinder.Read(valueProperty, valueType);
            }

            return Activator.CreateInstance(propertyType, key, value);
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