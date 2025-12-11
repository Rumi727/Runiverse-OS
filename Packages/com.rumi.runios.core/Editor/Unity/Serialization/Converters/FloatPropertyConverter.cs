#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(float))]
    public class FloatPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.floatValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.floatValue = (float)(value ?? 0f);
    }
}