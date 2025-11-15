#nullable enable
namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(bool))]
    public class BoolPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.boolValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.boolValue = (bool)(value ?? false);
    }
}