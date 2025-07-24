#nullable enable
using RuniEngine.IO;
using RuniEngine.Resource;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static char CharFieldLayout(char value) => CharField(EditorGUILayout.GetControlRect(), value);
        public static char CharFieldLayout(string label, char value) => CharField(EditorGUILayout.GetControlRect(), label, value);
        public static char CharFieldLayout(GUIContent label, char value) => CharField(EditorGUILayout.GetControlRect(), label, value);

        public static char CharField(Rect position, char value) => DoCharField(position, value);
        public static char CharField(Rect position, string label, char value) => CharField(position, new GUIContent(label), value);
        public static char CharField(Rect position, GUIContent label, char value) => DoCharField(EditorGUI.PrefixLabel(position, label), value);

        static char DoCharField(Rect position, char value)
        {
            string stringValue;
            if (value == '\n')
                stringValue = "\\n";
            else if (value == '\r')
                stringValue = "\\r";
            else if (value == '\t')
                stringValue = "\\t";
            else if (value == '\v')
                stringValue = "\\v";
            else if (value == '\0')
                stringValue = "\\0";
            else if (value == '\a')
                stringValue = "\\a";
            else if (value == '\b')
                stringValue = "\\b";
            else if (value == '\f')
                stringValue = "\\f";
            else if (char.IsControl(value))
                stringValue = $"\\u{(int)value:X4}";
            else
                stringValue = value.ToString();

            EditorGUI.BeginChangeCheck();
            stringValue = EditorGUI.TextField(position, stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (stringValue.StartsWith("\\u"))
                {
                    if (stringValue.Length == 6 && uint.TryParse(stringValue.Substring(2), NumberStyles.HexNumber, null, out uint result))
                        return (char)result;
                }
                else if (stringValue == "\\n")
                    return '\n';
                else if (stringValue == "\\r")
                    return '\r';
                else if (stringValue == "\\t")
                    return '\t';
                else if (stringValue == "\\v")
                    return '\v';
                else if (stringValue == "\\0")
                    return '\0';
                else if (stringValue == "\\a")
                    return '\a';
                else if (stringValue == "\\b")
                    return '\b';
                else if (stringValue == "\\f")
                    return '\f';
                else if (char.TryParse(stringValue, out char result))
                    return result;
            }

            return value;
        }



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

                    intValue = intValue.Repeat(minValue, maxValue);

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

                    longValue = longValue.Repeat(minValue, maxValue);

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

                    floatValue = floatValue.Repeat(minValue, maxValue);

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

                    doubleValue = doubleValue.Repeat(minValue, maxValue);

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



        public static FileExtension FileExtensionFieldLayout(FileExtension value) => FileExtensionField(EditorGUILayout.GetControlRect(), value);
        public static FileExtension FileExtensionFieldLayout(string label, FileExtension value) => FileExtensionField(EditorGUILayout.GetControlRect(), label, value);
        public static FileExtension FileExtensionFieldLayout(GUIContent label, FileExtension value) => FileExtensionField(EditorGUILayout.GetControlRect(), label, value);

        public static FileExtension FileExtensionField(Rect position, FileExtension value) => DoFileExtensionField(position, value);
        public static FileExtension FileExtensionField(Rect position, string label, FileExtension value) => FileExtensionField(position, new GUIContent(label), value);
        public static FileExtension FileExtensionField(Rect position, GUIContent label, FileExtension value) => DoFileExtensionField(EditorGUI.PrefixLabel(position, label), value);

        static FileExtension DoFileExtensionField(Rect position, FileExtension value)
        {
            BeginIndentLevel(0);

            {
                int leftPadding = EditorStyles.textField.padding.left;
                EditorStyles.textField.padding.left = 6;

                string textValue = EditorGUI.TextField(position, value.value.Length > 0 ? value.value.Substring(1) : string.Empty);
                if (textValue.Length > 0)
                    textValue = FileExtension.extensionSeparatorChar + textValue;

                value = textValue;

                EditorStyles.textField.padding.left = leftPadding;
            }

            EndIndentLevel();

            position.x += 2;
            position.y += 1;

            if (value.value.Length > 0 || APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                GUI.Label(position, FileExtension.extensionSeparatorChar.ToString(), EditorStyles.label);

            return value;
        }



        public static Identifier IdentifierFieldLayout(Identifier value) => IdentifierField(EditorGUILayout.GetControlRect(), value);
        public static Identifier IdentifierFieldLayout(string label, Identifier value) => IdentifierField(EditorGUILayout.GetControlRect(), label, value);
        public static Identifier IdentifierFieldLayout(GUIContent label, Identifier value) => IdentifierField(EditorGUILayout.GetControlRect(), label, value);

        public static Identifier IdentifierField(Rect position, Identifier value) => DoIdentifierField(position, value);
        public static Identifier IdentifierField(Rect position, string label, Identifier value) => IdentifierField(position, new GUIContent(label), value);
        public static Identifier IdentifierField(Rect position, GUIContent label, Identifier value)
        {
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3);

            return DoIdentifierField(position, value);
        }

        static Identifier DoIdentifierField(Rect position, Identifier value)
        {
            BeginIndentLevel(0);
            float fieldWidth = (position.width - (2 * 4) - (4 * 2)) / 3f;

            {
                position.width = fieldWidth;
                value.nameSpace = EditorGUI.TextField(position, value.nameSpace);
                position.x += position.width + 4;
            }

            {
                position.width = 8;
                position.x -= 4;

                GUI.Label(position, Identifier.separator.ToString());

                position.x += position.width;
            }
            
            {
                position.width = (fieldWidth * 2) + 8;
                value.path = EditorGUI.TextField(position, value.path);
            }

            EndIndentLevel();
            return value;
        }



        public static RectOffset RectOffsetFieldLayout(RectOffset value) => RectOffsetField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value);
        public static RectOffset RectOffsetFieldLayout(string label, RectOffset value) => RectOffsetField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value);
        public static RectOffset RectOffsetFieldLayout(GUIContent label, RectOffset value) => RectOffsetField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value);

        public static RectOffset RectOffsetField(Rect position, RectOffset value) => DoRectOffsetField(position, value);
        public static RectOffset RectOffsetField(Rect position, string label, RectOffset value) => RectOffsetField(position, new GUIContent(label), value);
        public static RectOffset RectOffsetField(Rect position, GUIContent label, RectOffset value)
        {
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);

            return DoRectOffsetField(position, value);
        }

        static readonly GUIContent[] rectOffsetLabels = new GUIContent[] { new GUIContent("L"), new GUIContent("R"), new GUIContent("T"), new GUIContent("B") };
        static readonly float[] rectOffsetValues = new float[4];
        static RectOffset DoRectOffsetField(Rect position, RectOffset value)
        {
            rectOffsetValues[0] = value.left;
            rectOffsetValues[1] = value.right;
            rectOffsetValues[2] = value.top;
            rectOffsetValues[3] = value.bottom;

            EditorGUI.MultiFloatField(position, rectOffsetLabels, rectOffsetValues);

            value.left = rectOffsetValues[0];
            value.right = rectOffsetValues[1];
            value.top = rectOffsetValues[2];
            value.bottom = rectOffsetValues[3];

            return value;
        }



        public static T? NullableFieldLayout<T>(T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value, drawAction, nullText);
        public static T? NullableFieldLayout<T>(string label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawAction, nullText);
        public static T? NullableFieldLayout<T>(GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawAction, nullText);

        public static T? NullableField<T>(Rect position, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => DoNullableField(position, GUIContent.none, value, drawAction, nullText);
        public static T? NullableField<T>(Rect position, string label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => NullableField(position, new GUIContent(label), value, drawAction, nullText);
        public static T? NullableField<T>(Rect position, GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct
        {
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);

            return DoNullableField(position, label, value, drawAction, nullText);
        }

        static T? DoNullableField<T>(Rect position, GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct
        {
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

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
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);

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
                    if (typeof(T).IsText())
                        value = (T)Convert.ChangeType(EditorGUI.TextField(fieldRect, label, nullText), typeof(T));
                    else
                    {
                        value = (T)PrimitiveField(fieldRect, label, default(T));

                        if (!APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                            GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                        else
                            GUI.Box(Rect.zero, GUIContent.none);
                    }
                }
                else
                    value = (T)PrimitiveField(fieldRect, label, value);

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

        public static T? InternalNullableToggleField<T>(T? value, Rect toggleRect)
        {
            BeginIndentLevel(0);

            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, value != null);
            if (EditorGUI.EndChangeCheck())
            {
                if (toggleValue)
                    value = (T?)typeof(T).GetDefaultValueNotNull();
                else
                    value = default;
            }

            EndIndentLevel();
            return value;
        }



        public static Vector4 Vector4FieldLayout(Vector4 value) => Vector4Field(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value);
        public static Vector4 Vector4FieldLayout(string label, Vector4 value) => Vector4Field(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value);
        public static Vector4 Vector4FieldLayout(GUIContent label, Vector4 value) => Vector4Field(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value);

        public static Vector4 Vector4Field(Rect position, Vector4 value) => DoVector4Field(position, value);
        public static Vector4 Vector4Field(Rect position, string label, Vector4 value) => Vector4Field(position, new GUIContent(label), value);
        public static Vector4 Vector4Field(Rect position, GUIContent label, Vector4 value)
        {
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4); // 2로 하면 크기 절반 줄어듬

            return DoVector4Field(position, value);
        }

        static readonly GUIContent[] vector4Labels = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") };
        static readonly float[] vector4Values = new float[4];
        static Vector4 DoVector4Field(Rect position, Vector4 value)
        {
            vector4Values[0] = value.x;
            vector4Values[1] = value.y;
            vector4Values[2] = value.z;
            vector4Values[3] = value.w;

            EditorGUI.MultiFloatField(position, vector4Labels, vector4Values);

            value.x = vector4Values[0];
            value.y = vector4Values[1];
            value.z = vector4Values[2];
            value.w = vector4Values[3];

            return value;
        }



        public static Version VersionFieldLayout(Version value) => VersionField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value);
        public static Version VersionFieldLayout(string label, Version value) => VersionField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value);
        public static Version VersionFieldLayout(GUIContent label, Version value) => VersionField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value);

        public static Version VersionField(Rect position, Version value) => DoVersionField(position, value);
        public static Version VersionField(Rect position, string label, Version value) => VersionField(position, new GUIContent(label), value);
        public static Version VersionField(Rect position, GUIContent label, Version value)
        {
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);

            return DoVersionField(position, value);
        }

        static Version DoVersionField(Rect position, Version value)
        {
            BeginIndentLevel(0);
            float fieldWidth = (position.width - (2 * 4) - (4 * 2)) / 3f;

            {
                position.width = fieldWidth;
                value.major = NullablePrimitiveField(position, value.major, "*");
                position.x += position.width + 4;
            }

            {
                position.width = 8;
                position.x -= 4;

                GUI.Label(position, ".");

                position.x += position.width;
                position.width += 4;
            }

            {
                position.width = fieldWidth.Floor();
                value.minor = NullablePrimitiveField(position, value.minor, "*");
                position.x += position.width + 4;
            }

            {
                position.width = 8;
                position.x -= 4;

                GUI.Label(position, ".");

                position.x += position.width;
                position.width += 4;
            }

            {
                position.width = fieldWidth.Ceil();
                value.patch = NullablePrimitiveField(position, value.patch, "*");
                position.x += position.width + 4;
            }

            EndIndentLevel();
            return value;
        }



        public static KeyValuePair<TKey, TValue> KeyValuePairFieldLayout<TKey, TValue>(KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairFieldLayout<TKey, TValue>(string label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairFieldLayout<TKey, TValue>(GUIContent label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawKeyAction, drawValueAction);

        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(Rect position, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => DoKeyValuePairField(position, value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(Rect position, string label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(position, new GUIContent(label), value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(Rect position, GUIContent label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction)
        {
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3); // 2로 하면 크기 절반 줄어듬

            return DoKeyValuePairField(position, value, drawKeyAction, drawValueAction);
        }

        static KeyValuePair<TKey, TValue> DoKeyValuePairField<TKey, TValue>(Rect position, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction)
        {
            BeginIndentLevel(0);
            float fieldWidth = (position.width - 15) / 2f;

            TKey valueKey = value.Key;
            TValue valueValue = value.Value;

            {
                position.width = fieldWidth;

                string keyLabel = GetTextOrKey("gui.key");

                BeginLabelWidth(keyLabel);
                EditorGUI.PrefixLabel(position, new GUIContent(keyLabel));
                valueKey = drawKeyAction.Invoke(position, valueKey);
                EndLabelWidth();

                position.x += position.width + 15;
            }

            {
                position.width = fieldWidth.Ceil();

                string valueLabel = GetTextOrKey("gui.value");

                BeginLabelWidth(valueLabel);
                EditorGUI.PrefixLabel(position, new GUIContent(valueLabel));
                valueValue = drawValueAction.Invoke(position, valueValue);
                EndLabelWidth();
            }

            EndIndentLevel();
            return KeyValuePair.Create(valueKey, valueValue);
        }
    }
}
