#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(RectInt))]
    public class RectIntPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.rectIntValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.rectIntValue = (RectInt)(value ?? new RectInt());
    }
}