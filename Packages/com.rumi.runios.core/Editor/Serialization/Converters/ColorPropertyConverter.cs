#nullable enable
namespace RuniOS.Editor.Serialization.Converters;

[CustomPropertyConverter(typeof(Color))]
public class ColorPropertyConverter : PropertyConverter
{
    public override object Read(SerializedProperty property, Type propertyType) => property.colorValue;
    public override void Write(SerializedProperty property, Type propertyType, object? value) => property.colorValue = (Color)(value ?? new Color());
}