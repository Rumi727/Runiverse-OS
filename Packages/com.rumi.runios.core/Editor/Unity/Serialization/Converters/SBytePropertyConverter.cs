#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(sbyte))]
    public class SBytePropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (sbyte)property.intValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.intValue = (int)(value ?? 0);
    }
}