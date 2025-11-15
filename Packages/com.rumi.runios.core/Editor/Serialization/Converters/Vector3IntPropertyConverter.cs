#nullable enable
namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Vector3Int))]
    public class Vector3IntPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.vector3IntValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector3IntValue = (Vector3Int)(value ?? new Vector3Int());
    }
}