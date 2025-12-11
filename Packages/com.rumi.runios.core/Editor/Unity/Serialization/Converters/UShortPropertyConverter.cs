#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(ushort))]
    public class UShortPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (ushort)property.uintValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.uintValue = (uint)(value ?? 0u);
    }
}