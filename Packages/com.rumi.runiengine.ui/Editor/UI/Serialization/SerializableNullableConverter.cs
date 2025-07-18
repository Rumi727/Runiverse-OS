#nullable enable
using RuniEngine.Editor.APIBridge.UnityEditor.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniEngine.Editor.UI.Serialization
{
    public sealed class SerializableNullableConverter<T> : UxmlAttributeConverter<SerializableNullable<T>> where T : struct
    {
        public override SerializableNullable<T> FromString(string value)
        {
            if (value == "null")
                return null;

            if (UxmlAttributeConverter.TryGetConverter(typeof(T), out IUxmlAttributeConverter converter))
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
