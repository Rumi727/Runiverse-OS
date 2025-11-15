#nullable enable
namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Vector2))]
    public class Vector2PropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.vector2Value;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector2Value = (Vector2)(value ?? new Vector2());
    }
}