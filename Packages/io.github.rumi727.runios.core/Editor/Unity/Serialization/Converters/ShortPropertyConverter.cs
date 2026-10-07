#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(short))]
    public class ShortPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (short)property.intValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.intValue = (int)(value ?? 0);
    }
}