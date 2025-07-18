#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SerializableNullable<>))]
    public class SerializableNullablePropertyDrawer : PropertyDrawer
    {
        SerializedProperty? field;
        SerializedProperty? toggle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            field ??= property.FindPropertyRelative("value");
            toggle ??= property.FindPropertyRelative("hasValue");

            Draw(position, field, toggle, label);
        }

        public static void Draw(Rect position, SerializedProperty field, SerializedProperty toggle, GUIContent label, string? customNullText = null)
        {
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            if (field == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent($"{TryGetText("serializable_nullable.invalid_serialization_type")}"));
                EditorGUI.PropertyField(toggleRect, toggle, GUIContent.none, false);

                return;
            }
            else if (toggle == null)
                return;

            if (field.IsTextField())
            {
                Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
                string nullText = customNullText ?? $"null ({field.type})";

                if (toggleRect.Contains(Event.current.mousePosition))
                {
                    fieldRect = GetPrefixLabelRect(fieldRect, label, out Rect? labelPosition);

                    if (labelPosition != null)
                        EditorGUI.LabelField(labelPosition.Value, label);

                    if (toggle.boolValue)
                        GUI.Box(fieldRect, field.boxedValue.ToString(), EditorStyles.textField);
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

                    if (field.propertyType == SerializedPropertyType.Float)
                    {
                        value = EditorGUI.DoubleField(fieldRect, label, 0);

                        if (!APIBridge.UnityEditor.EditorGUI.HasKeyboardFocus(APIBridge.UnityEditor.EditorGUIUtility.s_LastControlID))
                            GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                        else
                            GUI.Box(Rect.zero, GUIContent.none);
                    }

                    if (field.propertyType == SerializedPropertyType.String)
                        value = EditorGUI.TextField(fieldRect, label, nullText);

                    if (EditorGUI.EndChangeCheck())
                    {
                        field.boxedValue = value;
                        toggle.boolValue = true;
                    }
                }
                else
                    EditorGUI.PropertyField(fieldRect, field, label, field.IsChildrenIncluded());

                EditorGUI.PropertyField(toggleRect, toggle, GUIContent.none, false);
            }
            else
            {
                if (!field.IsChildrenIncluded() && field.propertyType != SerializedPropertyType.Rect)
                    position.width -= toggleWidth + 4;

                EditorGUI.PropertyField(toggleRect, toggle, GUIContent.none, false);

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
