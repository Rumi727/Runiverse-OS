#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(char))]
    public class CharPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (char)property.intValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.intValue = (char)(value ?? 0);
    }
}