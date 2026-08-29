#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(Bounds))]
    public class BoundsPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.boundsValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.boundsValue = (Bounds)(value ?? new Bounds());
    }
}