#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Editor.Unity.Drawers;

namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(SerializableKeyValuePair<,>), true)]
    public class SerializableKeyValuePairPropertyConverter<TKey, TValue> : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            TKey key = default!;
            (SerializedProperty? keyProperty, SerializedProperty? valueProperty) = SerializableKeyValuePairPropertyDrawer.GetChildProperty(property);
            if (keyProperty != null)
            {
                PropertyConverter? keyBinder = FindConverter<TKey>();
                if (keyBinder != null)
                    key = (TKey)keyBinder.Read(keyProperty, typeof(TKey))!;
            }

            TValue value = default!;
            if (valueProperty != null)
            {
                PropertyConverter? valueBinder = FindConverter(typeof(TValue));
                if (valueBinder != null)
                    value = (TValue)valueBinder.Read(valueProperty, typeof(TValue))!;
            }

            return new SerializableKeyValuePair<TKey, TValue>(key, value);
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is not ISerializableKeyValuePair<TKey, TValue> pair)
                return;

            (SerializedProperty? keyProperty, SerializedProperty? valueProperty) = SerializableKeyValuePairPropertyDrawer.GetChildProperty(property);
            if (keyProperty != null)
                FindConverter<TKey>()?.Write(keyProperty, typeof(TKey), pair.Key);

            if (valueProperty != null)
                FindConverter(typeof(TValue))?.Write(valueProperty, typeof(TValue), pair.Value);
        }
    }
}