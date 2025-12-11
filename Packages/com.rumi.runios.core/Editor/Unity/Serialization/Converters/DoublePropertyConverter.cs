#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(double))]
    public class DoublePropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.doubleValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.doubleValue = (double)(value ?? 0d);
    }
}