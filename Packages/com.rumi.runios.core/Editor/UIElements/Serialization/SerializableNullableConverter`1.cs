#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class SerializableNullableConverter<T> : UxmlAttributeConverter<SerializableNullable<T>> where T : struct
    {
        public override SerializableNullable<T> FromString(string value)
        {
            if (value == "null")
                return null;

            if (UxmlAttributeConverterBridge.TryGetConverter(typeof(T), out IUxmlAttributeConverterBridge converter))
                return (T)converter.FromString(value, CreationContext.Default);
            else
                return null;
        }

        public override string ToString(SerializableNullable<T> value)
        {
            if (value == null)
                return "null";

            return value.ToString();
        }
    }
}
