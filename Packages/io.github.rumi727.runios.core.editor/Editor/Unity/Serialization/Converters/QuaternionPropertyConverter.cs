#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(Quaternion))]
    public class QuaternionPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.quaternionValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.quaternionValue = (Quaternion)(value ?? new Quaternion());
    }
}