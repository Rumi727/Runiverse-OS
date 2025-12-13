#nullable enable
namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static T PrimitiveFieldLayout<T>(T value) where T : struct => PrimitiveField(EditorGUILayout.GetControlRect(), value);
        public static T PrimitiveFieldLayout<T>(string label, T value) where T : struct => PrimitiveField(EditorGUILayout.GetControlRect(), label, value);
        public static T PrimitiveFieldLayout<T>(GUIContent label, T value) where T : struct => PrimitiveField(EditorGUILayout.GetControlRect(), label, value);

        public static T PrimitiveField<T>(Rect position, T value) where T : struct => (T)DoPrimitiveField(position, value);
        public static T PrimitiveField<T>(Rect position, string label, T value) where T : struct => PrimitiveField(position, new GUIContent(label), value);
        public static T PrimitiveField<T>(Rect position, GUIContent label, T value) where T : struct => (T)DoPrimitiveField(EditorGUI.PrefixLabel(position, label), value);

        public static object PrimitiveFieldLayout(object value) => PrimitiveField(EditorGUILayout.GetControlRect(), value);
        public static object PrimitiveFieldLayout(string label, object value) => PrimitiveField(EditorGUILayout.GetControlRect(), label, value);
        public static object PrimitiveFieldLayout(GUIContent label, object value) => PrimitiveField(EditorGUILayout.GetControlRect(), label, value);

        public static object PrimitiveField(Rect position, object value) => DoPrimitiveField(position, value);
        public static object PrimitiveField(Rect position, string label, object value) => PrimitiveField(position, new GUIContent(label), value);
        public static object PrimitiveField(Rect position, GUIContent label, object value) => DoPrimitiveField(EditorGUI.PrefixLabel(position, label), value);

        static object DoPrimitiveField(Rect position, object value)
        {
            Type type = value.GetType();
            if (type == typeof(bool))
                return EditorGUI.Toggle(position, (bool)value);
            else if (type.IsNumeric())
            {
                if (type.IsAssignableToInt())
                {
                    EditorGUI.BeginChangeCheck();

                    int intValue = EditorGUI.IntField(position, Convert.ToInt32(value));

                    int minValue = Convert.ToInt32(type.GetMinValue());
                    int maxValue = Convert.ToInt32(type.GetMaxValue());

                    intValue = intValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(intValue, type);

                    return value;
                }
                else if (type.IsAssignableToLong())
                {
                    EditorGUI.BeginChangeCheck();

                    long longValue = EditorGUI.LongField(position, Convert.ToInt64(value));

                    long minValue = Convert.ToInt64(type.GetMinValue());
                    long maxValue = Convert.ToInt64(type.GetMaxValue());

                    longValue = longValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(longValue, type);

                    return value;
                }
                else if (type.IsAssignableToFloat())
                {
                    EditorGUI.BeginChangeCheck();

                    float floatValue = EditorGUI.FloatField(position, Convert.ToInt32(value));

                    float minValue = Convert.ToSingle(type.GetMinValue());
                    float maxValue = Convert.ToSingle(type.GetMaxValue());

                    floatValue = floatValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(floatValue, type);

                    return value;
                }
                else if (type.IsAssignableToDouble())
                {
                    EditorGUI.BeginChangeCheck();

                    double doubleValue = EditorGUI.DoubleField(position, Convert.ToInt32(value));

                    double minValue = Convert.ToDouble(type.GetMinValue());
                    double maxValue = Convert.ToDouble(type.GetMaxValue());

                    doubleValue = doubleValue.Clamp(minValue, maxValue);

                    if (EditorGUI.EndChangeCheck())
                        value = Convert.ChangeType(doubleValue, type);

                    return value;
                }
            }
            else if (type == typeof(char))
                return CharField(position, (char)value);
            else if (type == typeof(string))
                return EditorGUI.TextField(position, (string)value);

            EditorGUI.LabelField(position, GetTextOrKey("gui.invalid_type"));
            return value;
        }
    }
}