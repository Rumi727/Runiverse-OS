#nullable enable
namespace RuniOS.Editor.Serialization.Converters;

[CustomPropertyConverter(typeof(byte))]
public class BytePropertyConverter : PropertyConverter
{
    public override object Read(SerializedProperty property, Type propertyType) => (byte)property.uintValue;
    public override void Write(SerializedProperty property, Type propertyType, object? value) => property.uintValue = (uint)(value ?? 0u);
}