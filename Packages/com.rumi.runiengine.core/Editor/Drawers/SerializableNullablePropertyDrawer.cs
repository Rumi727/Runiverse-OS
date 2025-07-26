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
                            switch (charValue)
                            {
                                case '\n':
                                    stringValue = "\\n";
                                    break;
                                case '\r':
                                    stringValue = "\\r";
                                    break;
                                case '\t':
                                    stringValue = "\\t";
                                    break;
                                case '\v':
                                    stringValue = "\\v";
                                    break;
                                case '\0':
                                    stringValue = "\\0";
                                    break;
                                case '\a':
                                    stringValue = "\\a";
                                    break;
                                case '\b':
                                    stringValue = "\\b";
                                    break;
                                case '\f':
                                    stringValue = "\\f";
                                    break;
                                default:
                                {
                                    if (char.IsControl(charValue))
                                        stringValue = $"\\u{(int)charValue:X4}";
                                    else
                                        stringValue = charValue.ToString();
                                    break;
                                }
                            }

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

                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (field.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                        {
                            value = EditorGUI.LongField(fieldRect, label, 0);

                            if (!APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                                GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                            else
                                GUI.Box(Rect.zero, GUIContent.none);
                            
                            break;
                        }
                        case SerializedPropertyType.Float:
                        {
                            value = EditorGUI.DoubleField(fieldRect, label, 0);

                            if (!APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                                GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                            else
                                GUI.Box(Rect.zero, GUIContent.none);
                            
                            break;
                        }
                        case SerializedPropertyType.Character:
                        {
                            string stringValue = EditorGUI.TextField(fieldRect, label, nullText);
                            if (char.TryParse(stringValue, out char result))
                                value = result;
                            
                            break;
                        }
                        case SerializedPropertyType.String:
                        {
                            value = EditorGUI.TextField(fieldRect, label, nullText);
                            break;
                        }
                    }

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
                if (!field.IsChildrenIncluded() && field.propertyType != SerializedPropertyType.Vector2 && field.propertyType != SerializedPropertyType.Rect)
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
