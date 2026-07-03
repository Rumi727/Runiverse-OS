#nullable enable
using RuniOS.Editor.Unity.Drawers;

namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Version))]
    public class VersionPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            (SerializedProperty majorProperty, SerializedProperty minorProperty, SerializedProperty patchProperty) = VersionPropertyDrawer.GetChildProperty(property);
                
            int? major = (SerializableNullable<int>)new SerializableNullablePropertyConverter<int>().Read(majorProperty, typeof(SerializableNullable<int>));
            int? minor = (SerializableNullable<int>)new SerializableNullablePropertyConverter<int>().Read(minorProperty, typeof(SerializableNullable<int>));
            int? patch = (SerializableNullable<int>)new SerializableNullablePropertyConverter<int>().Read(patchProperty, typeof(SerializableNullable<int>));

            return new Version(major, minor, patch);
        }

        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is Version version)
            {
                (SerializedProperty majorProperty, SerializedProperty minorProperty, SerializedProperty patchProperty) = VersionPropertyDrawer.GetChildProperty(property);
                
                new SerializableNullablePropertyConverter<int>().Write(majorProperty, typeof(SerializableNullable<int>), new SerializableNullable<int>(version.major));
                new SerializableNullablePropertyConverter<int>().Write(minorProperty, typeof(SerializableNullable<int>), new SerializableNullable<int>(version.minor));
                new SerializableNullablePropertyConverter<int>().Write(patchProperty, typeof(SerializableNullable<int>), new SerializableNullable<int>(version.patch));
            }
        }
    }
}