#nullable enable
namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(uint))]
    public class UIntPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.uintValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.uintValue = (uint)(value ?? 0u);
    }
}