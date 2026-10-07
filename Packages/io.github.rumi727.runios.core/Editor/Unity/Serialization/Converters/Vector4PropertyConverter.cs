#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(Vector4))]
    public class Vector4PropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.vector4Value;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector4Value = (Vector4)(value ?? new Vector4());
    }
}