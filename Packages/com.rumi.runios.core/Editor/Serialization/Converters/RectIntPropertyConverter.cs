#nullable enable
namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(RectInt))]
    public class RectIntPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.rectIntValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.rectIntValue = (RectInt)(value ?? new RectInt());
    }
}