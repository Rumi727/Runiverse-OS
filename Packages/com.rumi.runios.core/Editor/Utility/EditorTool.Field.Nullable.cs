#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static T? NullableFieldLayout<T>(T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value, drawAction, nullText);
        public static T? NullableFieldLayout<T>(string label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawAction, nullText);
        public static T? NullableFieldLayout<T>(GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawAction, nullText);

        public static T? NullableField<T>(Rect position, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => DoNullableField(position, GUIContent.none, value, drawAction, nullText);
        public static T? NullableField<T>(Rect position, string label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(position, new GUIContent(label), value, drawAction, nullText);
        public static T? NullableField<T>(Rect position, GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct
        {
            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 4);

            return DoNullableField(position, label, value, drawAction, nullText);
        }

        static T? DoNullableField<T>(Rect position, GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct
        {
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);
            position.width -= toggleWidth + 4;

            nullText ??= $"null ({typeof(T).GetTypeDisplayName()})";

            InternalNullableToggleField(value, toggleRect);

            if (value != null)
                return drawAction.Invoke(position, value.Value);
            else
            {
                EditorGUI.LabelField(position, label, new GUIContent(nullText));
                return value;
            }
        }



        public static T? NullablePrimitiveFieldLayout<T>(T? value, string? nullText = null) where T : struct => NullablePrimitiveField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value, nullText);
        public static T? NullablePrimitiveFieldLayout<T>(string label, T? value, string? nullText = null) where T : struct => NullablePrimitiveField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, nullText);
        public static T? NullablePrimitiveFieldLayout<T>(GUIContent label, T? value, string? nullText = null) where T : struct => NullablePrimitiveField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, nullText);

        public static T? NullablePrimitiveField<T>(Rect position, T? value, string? nullText = null) where T : struct => DoNullablePrimitiveField(position, GUIContent.none, value, nullText);
        public static T? NullablePrimitiveField<T>(Rect position, string label, T? value, string? nullText = null) where T : struct => NullablePrimitiveField(position, new GUIContent(label), value, nullText);
        public static T? NullablePrimitiveField<T>(Rect position, GUIContent label, T? value, string? nullText = null) where T : struct
        {
            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 4);
        
            return DoNullablePrimitiveField(position, label, value, nullText);
        }

        static T? DoNullablePrimitiveField<T>(Rect position, GUIContent label, T? value, string? nullText = null) where T : struct
        {
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            nullText ??= $"null ({typeof(T).GetTypeDisplayName()})";

            if (typeof(T).IsTextField())
            {
                Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
                if (toggleRect.Contains(Event.current.mousePosition))
                {
                    fieldRect = GetPrefixLabelRect(fieldRect, label, out Rect? labelPosition);

                    if (labelPosition != null)
                    {
                        BeginIndentLevel(0);
                        EditorGUI.LabelField(labelPosition.Value, label);
                        EndIndentLevel();
                    }

                    if (value != null)
                        GUI.Box(fieldRect, value.ToString(), EditorStyles.textField);
                    else
                        GUI.Box(fieldRect, nullText, EditorStyles.textField);
                }
                else if (value == null)
                {
                    EditorGUI.BeginChangeCheck();

                    T primitiveValue;
                    if (typeof(T).IsText())
                        primitiveValue = (T)Convert.ChangeType(EditorGUI.TextField(fieldRect, label, nullText), typeof(T));
                    else
                    {
                        primitiveValue = PrimitiveField(fieldRect, label, default(T));

                        if (!EditorGUIBridge.HasKeyboardFocus(EditorGUIUtilityBridge.s_LastControlID))
                            GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                        else
                            GUI.Box(Rect.zero, GUIContent.none);
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                        value = primitiveValue;
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    T primitiveValue = PrimitiveField(fieldRect, label, value.Value);
                    if (EditorGUI.EndChangeCheck())
                        value = primitiveValue;
                }

                value = InternalNullableToggleField(value, toggleRect);
            }
            else
            {
                value = InternalNullableToggleField(value, toggleRect);

                if (value != null)
                    value = (T)PrimitiveField(position, label, value);
                else
                    EditorGUI.LabelField(position, label, new GUIContent(nullText));
            }

            return value;
        }

        public static T? InternalNullableToggleField<T>(T? value, Rect toggleRect, Func<T>? constructor = null) where T : struct
        {
            BeginIndentLevel(0);

            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, value != null);
            if (EditorGUI.EndChangeCheck())
            {
                if (toggleValue)
                    value = constructor?.Invoke() ?? (T)typeof(T).GetDefaultValueNotNull();
                else
                    value = null;
            }

            EndIndentLevel();
            return value;
        }
    }
}