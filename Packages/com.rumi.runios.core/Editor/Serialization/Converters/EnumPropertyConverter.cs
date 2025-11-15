#nullable enable
namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Enum), true)]
    public class EnumPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => Enum.ToObject(propertyType, property.numericType switch
        {
            SerializedPropertyNumericType.Int8 => (sbyte)property.intValue,
            SerializedPropertyNumericType.UInt8 => (byte)property.uintValue,
            SerializedPropertyNumericType.Int16 => (short)property.intValue,
            SerializedPropertyNumericType.UInt16 => (ushort)property.uintValue,
            SerializedPropertyNumericType.UInt32 => property.uintValue,
            SerializedPropertyNumericType.Int64 => property.longValue,
            SerializedPropertyNumericType.UInt64 => property.ulongValue,
            _ => property.intValue
        });

        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            switch (property.numericType)
            {
                case SerializedPropertyNumericType.Int8:
                    property.intValue = Convert.ToSByte((Enum)(value ?? 0));
                    break;
                case SerializedPropertyNumericType.UInt8:
                    property.uintValue = Convert.ToByte(value ?? 0u);
                    break;
                case SerializedPropertyNumericType.Int16:
                    property.intValue = Convert.ToInt16(value ?? 0);
                    break;
                case SerializedPropertyNumericType.UInt16:
                    property.uintValue = Convert.ToUInt16(value ?? 0u);
                    break;
                case SerializedPropertyNumericType.Int32:
                    property.intValue = Convert.ToInt32(value ?? 0);
                    break;
                case SerializedPropertyNumericType.UInt32:
                    property.uintValue = Convert.ToUInt16(value ?? 0u);
                    break;
                case SerializedPropertyNumericType.Int64:
                    property.longValue = Convert.ToInt64(value ?? 0);
                    break;
                case SerializedPropertyNumericType.UInt64:
                    property.ulongValue = Convert.ToUInt16(value ?? 0uL);
                    break;
                default:
                    property.intValue = Convert.ToInt32(value);
                    break;
            }
        }
    }
}