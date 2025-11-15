#nullable enable
using RuniOS.Editor.IMGUI.Drawers;

namespace RuniOS.Editor.Serialization.Converters;

[CustomPropertyConverter(typeof(SerializableType))]
public class SerializableTypePropertyConverter : PropertyConverter
{
    public override object Read(SerializedProperty property, Type propertyType) => new SerializableType(TypeUtility.DeserializeFromString(SerializableTypePropertyDrawer.GetChildProperty(property).stringValue));
        
    public override void Write(SerializedProperty property, Type propertyType, object? value)
    {
        if (value is SerializableType serializableType)
            SerializableTypePropertyDrawer.GetChildProperty(property).stringValue = serializableType.value?.SerializeToString() ?? string.Empty;
    }
}