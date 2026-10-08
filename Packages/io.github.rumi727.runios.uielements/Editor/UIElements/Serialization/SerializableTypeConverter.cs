#nullable enable
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class SerializableTypeConverter : UxmlAttributeConverter<SerializableType>
    {
        public override SerializableType FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            
            return new SerializableType(TypeUtility.DeserializeFromString(value));
        }
        
        public override string ToString(SerializableType value) => value.value?.SerializeToString() ?? string.Empty;
    }
}