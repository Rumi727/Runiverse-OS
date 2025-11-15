#nullable enable
namespace RuniOS.Editor.Serialization.Converters;

[CustomPropertyConverter(typeof(Vector2Int))]
public class Vector2IntPropertyConverter : PropertyConverter
{
    public override object Read(SerializedProperty property, Type propertyType) => property.vector2IntValue;
    public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector2IntValue = (Vector2Int)(value ?? new Vector2Int());
}