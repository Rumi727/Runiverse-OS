#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(Rect))]
    public class RectPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.rectValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.rectValue = (Rect)(value ?? new Rect());
    }
}