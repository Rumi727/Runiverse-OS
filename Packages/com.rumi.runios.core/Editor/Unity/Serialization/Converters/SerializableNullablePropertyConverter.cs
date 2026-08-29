#nullable enable
using RuniOS.Editor.Unity.Drawers;

namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(SerializableNullable<>), true)]
    public class SerializableNullablePropertyConverter<T> : PropertyConverter where T : struct
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (toggle is not { boolValue: true })
                return new SerializableNullable<T>();
            
            PropertyConverter? valueBinder = FindConverter<T>();
            if (field == null || valueBinder == null)
                return new SerializableNullable<T>(default);

            return new SerializableNullable<T>((T)valueBinder.Read(field, typeof(T))!);
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is not SerializableNullable<T> nullable)
                return;

            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (toggle == null)
                return;
            
            toggle.boolValue = nullable.HasValue;

            bool hasValue = nullable.HasValue;
            if (field != null)
                FindConverter<T>()?.Write(field, typeof(T), hasValue ? nullable.Value : default);
        }
    }
}