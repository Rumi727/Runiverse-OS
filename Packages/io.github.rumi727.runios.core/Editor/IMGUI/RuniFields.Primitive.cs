#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static T PrimitiveField<T>(Rect position, T value) where T : struct => (T)DoPrimitiveField(position, value);
        public static T PrimitiveField<T>(Rect position, string label, T value) where T : struct => PrimitiveField(position, new GUIContent(label), value);
        public static T PrimitiveField<T>(Rect position, GUIContent label, T value) where T : struct => (T)DoPrimitiveField(EditorGUI.PrefixLabel(position, label), value);

        public static object PrimitiveField(Rect position, object value) => DoPrimitiveField(position, value);
        public static object PrimitiveField(Rect position, string label, object value) => PrimitiveField(position, new GUIContent(label), value);
        public static object PrimitiveField(Rect position, GUIContent label, object value) => DoPrimitiveField(EditorGUI.PrefixLabel(position, label), value);

        static object DoPrimitiveField(Rect position, object value)
        {
            Type type = value.GetType();
            if (type == typeof(bool))
                return EditorGUI.Toggle(position, (bool)value);
            else if (type.IsNumeric)
            {
                if (type.IsExactlyRepresentableAsInt32)
                {
                    EditorGUI.BeginChangeCheck();

                    int intValue = EditorGUI.IntField(position, Convert.ToInt32(value));

                    int minValue = Convert.ToInt32(type.NumericMinValue);
                    int maxValue = Convert.ToInt32(type.NumericMaxValue);

                    intValue = intValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(intValue, type);

                    return value;
                }
                else if (type.IsExactlyRepresentableAsInt64)
                {
                    EditorGUI.BeginChangeCheck();

                    long longValue = EditorGUI.LongField(position, Convert.ToInt64(value));

                    long minValue = Convert.ToInt64(type.NumericMinValue);
                    long maxValue = Convert.ToInt64(type.NumericMaxValue);

                    longValue = longValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(longValue, type);

                    return value;
                }
                else if (type == typeof(ulong))
                {
                    EditorGUI.BeginChangeCheck();

                    long longValue = EditorGUI.LongField(position, ((ulong)value).ClampToLong());

                    if (EditorGUI.EndChangeCheck())
                        value = longValue.ClampToULong();

                    return value;
                }
                else if (type.IsExactlyRepresentableAsSingle)
                {
                    EditorGUI.BeginChangeCheck();

                    float floatValue = EditorGUI.FloatField(position, Convert.ToSingle(value));

                    float minValue = Convert.ToSingle(type.NumericMinValue);
                    float maxValue = Convert.ToSingle(type.NumericMaxValue);

                    floatValue = floatValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(floatValue, type);

                    return value;
                }
                else if (type.IsExactlyRepresentableAsDouble)
                {
                    EditorGUI.BeginChangeCheck();

                    double doubleValue = EditorGUI.DoubleField(position, Convert.ToDouble(value));

                    double minValue = Convert.ToDouble(type.NumericMinValue);
                    double maxValue = Convert.ToDouble(type.NumericMaxValue);

                    doubleValue = doubleValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(doubleValue, type);

                    return value;
                }
                else if (type == typeof(decimal))
                {
                    EditorGUI.BeginChangeCheck();

                    double doubleValue = EditorGUI.DoubleField(position, ((decimal)value).ClampToDouble());

                    if (EditorGUI.EndChangeCheck())
                        value = doubleValue.ClampToDecimal();

                    return value;
                }
            }
            else if (type == typeof(char))
                return CharField(position, (char)value);
            else if (type == typeof(string))
                return EditorGUI.TextField(position, (string)value);
            else if (type.IsEnum)
            {
                Enum enumValue = (Enum)value;
                if (enumValue.IsFlags)
                    return EditorGUI.EnumFlagsField(position, enumValue);
                else
                    return EditorGUI.EnumPopup(position, enumValue);
            }

            EditorGUI.LabelField(position, GetTextOrKey("gui.invalid_type"));
            return value;
        }
    }
}
