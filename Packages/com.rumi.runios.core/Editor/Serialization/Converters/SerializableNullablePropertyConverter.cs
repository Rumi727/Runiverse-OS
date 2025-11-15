#nullable enable
using RuniOS.Editor.IMGUI.Drawers;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(ISerializableNullable<>), true)]
    public class SerializableNullablePropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            object instance = Activator.CreateInstance(propertyType);
            Type? underlyingType = SerializableNullable.GetUnderlyingType(propertyType);
            if (underlyingType == null)
                return instance;
            
            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (toggle is not { boolValue: true })
                return instance;
            
            PropertyConverter? valueBinder = FindConverter(underlyingType);
            if (field == null || valueBinder == null)
                return Activator.CreateInstance(propertyType, underlyingType.GetDefaultValueNotNull());

            object? value = valueBinder.Read(field, underlyingType);
            if (value == null)
                return instance;

            return Activator.CreateInstance(propertyType, value);
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? nullable)
        {
            Type? underlyingType = SerializableNullable.GetUnderlyingType(propertyType);
            if (underlyingType == null)
                return;
            
            (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);
            if (toggle == null)
                return;
            
            bool hasValue = (bool)(AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfHasValue)?.GetValue(nullable) ?? false);
            if (field != null)
            {
                object? value = Activator.CreateInstance(underlyingType);
                if (hasValue)
                    value = AccessUtility.DeclaredProperty(propertyType, SerializableNullable.nameOfValue)?.GetValue(nullable);
                
                FindConverter(underlyingType)?.Write(field, underlyingType, value);
            }

            toggle.boolValue = hasValue;
        }
    }
}