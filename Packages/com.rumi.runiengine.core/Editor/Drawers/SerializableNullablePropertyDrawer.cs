#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ISerializableNullable<>), true)]
    public class SerializableNullablePropertyDrawer : PropertyDrawer
    {
        SerializedProperty? field;
        SerializedProperty? toggle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            field ??= property.FindPropertyRelative("value");
            toggle ??= property.FindPropertyRelative("hasValue");

            EditorGUI.BeginProperty(position, label, property);
            Draw(position, field, toggle, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty? field, SerializedProperty? toggle, GUIContent label, string? customNullText = null)
        {
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            if (field == null && toggle != null)
            {
                EditorGUI.LabelField(position, label, new GUIContent($"{GetTextOrKey("serializable_nullable.invalid_serialization_type")}"));

                int indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                EditorGUI.indentLevel = indentLevel;

                return;
            }
            else if (field == null || toggle == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent($"{GetTextOrKey("serializable_nullable.invalid_serialization_type")}"));
                return;
            }

            if (field.IsTextField())
            {
                Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
                string nullText = customNullText ?? $"null ({field.type})";

                if (toggleRect.Contains(Event.current.mousePosition))
                {
                    fieldRect = GetPrefixLabelRect(fieldRect, label, out Rect? labelPosition);

                    if (labelPosition != null)
                    {
                        BeginIndentLevel(0);
                        EditorGUI.LabelField(labelPosition.Value, label);
                        EndIndentLevel();
                    }

                    if (toggle.boolValue)
                    {
                        if (field.propertyType == SerializedPropertyType.Character)
                        {
                            char charValue = (char)(ushort)field.boxedValue;
                            string stringValue;
                            if (charValue == '\n')
                                stringValue = "\\n";
                            else if (charValue == '\r')
                                stringValue = "\\r";
                            else if (charValue == '\t')
                                stringValue = "\\t";
                            else if (charValue == '\v')
                                stringValue = "\\v";
                            else if (charValue == '\0')
                                stringValue = "\\0";
                            else if (charValue == '\a')
                                stringValue = "\\a";
                            else if (charValue == '\b')
                                stringValue = "\\b";
                            else if (charValue == '\f')
                                stringValue = "\\f";
                            else if (char.IsControl(charValue))
                                stringValue = $"\\u{(int)charValue:X4}";
                            else
                                stringValue = charValue.ToString();

                            GUI.Box(fieldRect, stringValue, EditorStyles.textField);
                        }
                        else
                            GUI.Box(fieldRect, field.boxedValue.ToString(), EditorStyles.textField);
                    }
                    else
                        GUI.Box(fieldRect, nullText, EditorStyles.textField);
                }
                else if (!toggle.boolValue)
                {
                    EditorGUI.BeginChangeCheck();

                    object? value = null;

                    if (field.propertyType == SerializedPropertyType.Integer)
                    {
                        value = EditorGUI.LongField(fieldRect, label, 0);

                        if (!APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                            GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                        else
                            GUI.Box(Rect.zero, GUIContent.none);
                    }
                    else if (field.propertyType == SerializedPropertyType.Float)
                    {
                        value = EditorGUI.DoubleField(fieldRect, label, 0);

                        if (!APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                            GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                        else
                            GUI.Box(Rect.zero, GUIContent.none);
                    }
                    else if (field.propertyType == SerializedPropertyType.Character)
                    {
                        string stringValue = EditorGUI.TextField(fieldRect, label, nullText);
                        if (char.TryParse(stringValue, out char result))
                            value = result;
                    }
                    else if (field.propertyType == SerializedPropertyType.String)
                        value = EditorGUI.TextField(fieldRect, label, nullText);

                    if (EditorGUI.EndChangeCheck())
                    {
                        field.boxedValue = value;
                        toggle.boolValue = true;
                    }
                }
                else
                    EditorGUI.PropertyField(fieldRect, field, label, field.IsChildrenIncluded());

                {
                    BeginIndentLevel(0);
                    toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                    EndIndentLevel();
                }
            }
            else
            {
                if (!field.IsChildrenIncluded() && field.propertyType != SerializedPropertyType.Rect)
                    position.width -= toggleWidth + 4;

                BeginIndentLevel(0);
                toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                EndIndentLevel();

                if (toggle.boolValue)
                    EditorGUI.PropertyField(position, field, label, field.IsChildrenIncluded());
                else
                    EditorGUI.LabelField(position, label, new GUIContent($"null ({field.type})"));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            field ??= property.FindPropertyRelative("value");
            toggle ??= property.FindPropertyRelative("hasValue");

            if (field != null && toggle != null && toggle.boolValue)
                return EditorGUI.GetPropertyHeight(field, label);
            else
                return EditorGUIUtility.singleLineHeight;
        }
    }
}
