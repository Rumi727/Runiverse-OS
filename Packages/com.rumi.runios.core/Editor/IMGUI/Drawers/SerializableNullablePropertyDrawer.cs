#nullable enable

using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.UIElements;
using RuniOS.Editor.UIElements.Nullables;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.IMGUI.Drawers
{
    [CustomPropertyDrawer(typeof(ISerializableNullable<>), true)]
    public class SerializableNullablePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => CreatePropertyGUI(property.GetPropertyTypeWithoutList(), property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static VisualElement CreatePropertyGUI(Type pairType, SerializedProperty property, string? nullText = null)
        {
            string invalidText = GetTextOrKey("serializable_nullable.invalid_serialization_type");
            
            Type? valueType = SerializableNullable.GetUnderlyingType(pairType);
            if (valueType == null)
                return new LabelField(property.GetFieldLabel(), invalidText);

            (SerializedProperty? fieldProperty, SerializedProperty? toggleProperty) = GetChildProperty(property);
            if (toggleProperty == null)
                return new LabelField(property.GetFieldLabel(), invalidText);
            
            Type fieldType = typeof(NullableField<>).MakeGenericType(valueType);
            Type descriptionType = typeof(RuniBaseCompositeField<>.AnonymousFieldDescription<>);

            if (fieldProperty == null)
            {
                descriptionType = descriptionType.MakeGenericType(pairType, typeof(LabelField));
                    
                object valueDescription = Activator.CreateInstance(descriptionType, SerializableNullable.nameOfInternalValue, new LabelField { value = invalidText });
                return ((VisualElement)Activator.CreateInstance(fieldType, property.GetFieldLabel(), valueDescription, invalidText)).SetProperty(property);
            }
            else
            {
                descriptionType = descriptionType.MakeGenericType(pairType, typeof(PropertyField));
                
                object valueDescription = Activator.CreateInstance(descriptionType, SerializableNullable.nameOfInternalValue, new PropertyField(fieldProperty));
                return ((VisualElement)Activator.CreateInstance(fieldType, property.GetFieldLabel(), valueDescription, nullText)).SetProperty(property);
            }
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label, string? customNullText = null)
        {
            (SerializedProperty? field, SerializedProperty? toggle) = GetChildProperty(property);
            
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            if (field == null && toggle != null)
            {
                EditorGUI.LabelField(position, label, new GUIContent(GetTextOrKey("serializable_nullable.invalid_serialization_type")));

                BeginIndentLevel(0);
                toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                EndIndentLevel();

                return;
            }
            else if (field == null || toggle == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent(GetTextOrKey("serializable_nullable.invalid_serialization_type")));
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

                            if (!EditorGUIBridge.HasKeyboardFocus(EditorGUIUtilityBridge.s_LastControlID))
                                GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                            else
                                GUI.Box(Rect.zero, GUIContent.none);
                            
                            break;
                        }
                        case SerializedPropertyType.Float:
                        {
                            value = EditorGUI.DoubleField(fieldRect, label, 0);

                            if (!EditorGUIBridge.HasKeyboardFocus(EditorGUIUtilityBridge.s_LastControlID))
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
                    EditorGUI.PropertyField(fieldRect, field, label, field.IsGeneric());

                {
                    BeginIndentLevel(0);
                    toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                    EndIndentLevel();
                }
            }
            else
            {
                position.width -= toggleWidth + 4;

                BeginIndentLevel(0);
                toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                EndIndentLevel();
                
                if (toggle.boolValue)
                    EditorGUI.PropertyField(position, field, label, field.IsGeneric());
                else
                    EditorGUI.LabelField(position, label, new GUIContent($"null ({field.type})"));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            (SerializedProperty? field, SerializedProperty? toggle) = GetChildProperty(property);
            if (field != null && toggle != null && toggle.boolValue)
                return EditorGUI.GetPropertyHeight(field, label);
            else
                return EditorGUIUtility.singleLineHeight;
        }

        public static (SerializedProperty? field, SerializedProperty? toggle) GetChildProperty(SerializedProperty property) => (property.FindPropertyRelative(SerializableNullable.nameOfInternalValue), property.FindPropertyRelative(SerializableNullable.nameOfInternalHasValue));
    }
}