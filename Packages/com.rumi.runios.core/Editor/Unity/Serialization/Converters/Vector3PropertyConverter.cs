#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Vector3))]
    public class Vector3PropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.vector3Value;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector3Value = (Vector3)(value ?? new Vector3());
    }
}