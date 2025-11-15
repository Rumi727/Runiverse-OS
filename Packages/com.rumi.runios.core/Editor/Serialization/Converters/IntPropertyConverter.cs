#nullable enable
namespace RuniOS.Editor.Serialization.Converters;

[CustomPropertyConverter(typeof(int))]
public class IntPropertyConverter : PropertyConverter
{
    public override object Read(SerializedProperty property, Type propertyType) => property.intValue;
    public override void Write(SerializedProperty property, Type propertyType, object? value) => property.intValue = (int)(value ?? 0);
}