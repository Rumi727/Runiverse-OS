#nullable enable

using RuniOS.Collections.Generic;
using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor.IMGUI.Drawers
{
    [CustomPropertyDrawer(typeof(ISerializableKeyValuePair), true)]
    [CustomPropertyDrawer(typeof(ISerializableKeyValuePair<,>), true)]
    public class SerializableKeyValuePairPropertyDrawer : PropertyDrawer
    {
        /*public override VisualElement? CreatePropertyGUI(SerializedProperty property)
        {
            Type pairType = property.GetPropertyTypeWithoutList();
            (Type? keyType, Type? valueType) = SerializableKeyValuePair.GetUnderlyingType(pairType);
            if (keyType == null || valueType == null)
                return null;

            (SerializedProperty? keyProperty, SerializedProperty? valueProperty) = GetChildProperty(property);
            if (keyProperty == null || valueProperty == null)
                return null;

            Type descriptionType = typeof(KeyValuePairField<>.AnonymousFieldDescription<>).MakeGenericType(pairType, typeof(Inspector));

            Inspector keyInspector = new Inspector(new SerializedPropertyElement(keyProperty));
            keyInspector.Rebuild();

            Inspector valueInspector = new Inspector(new SerializedPropertyElement(valueProperty));
            valueInspector.Rebuild();

            object keyDescription = Activator.CreateInstance(descriptionType, SerializableKeyValuePair.nameOfInternalKey, keyInspector);
            object valueDescription = Activator.CreateInstance(descriptionType, SerializableKeyValuePair.nameOfInternalValue, valueInspector);

            Type fieldType = typeof(KeyValuePairField<>).MakeGenericType(pairType);
            VisualElement element = ((VisualElement)Activator.CreateInstance(fieldType, keyDescription, valueDescription)).SetProperty(property, false);

            return element;
        }*/

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            (SerializedProperty? key, SerializedProperty? value) = GetChildProperty(property);
            if (key == null)
            {
                EditorGUI.LabelField(position, label, GetTextOrKey("serializable_key_value_pair_property_drawer.not_found.key"));
                return;
            }
            else if (value == null)
            {
                EditorGUI.LabelField(position, label, GetTextOrKey("serializable_key_value_pair_property_drawer.not_found.value"));
                return;
            }

            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 3); // 2로 하면 크기 절반 줄어듬
            
            BeginIndentLevel(0);
            float fieldWidth = (position.width - 15) / 2f;

            {
                string keyLabel = GetTextOrKey("gui.key");
                GUIContent keyLabelContent = new GUIContent(keyLabel);
                
                position.width = fieldWidth;
                position.height = EditorGUI.GetPropertyHeight(key, keyLabelContent);


                BeginLabelWidth(keyLabel);
                EditorGUI.PropertyField(position, key, keyLabelContent);
                EndLabelWidth();

                position.x += position.width + 15;
            }

            {
                string valueLabel = GetTextOrKey("gui.value");
                GUIContent valueLabelContent = new GUIContent(valueLabel);
                
                position.width = fieldWidth.Ceil();
                position.height = EditorGUI.GetPropertyHeight(value, valueLabelContent);

                BeginLabelWidth(valueLabel);
                EditorGUI.PropertyField(position, value, valueLabelContent);
                EndLabelWidth();
            }

            EndIndentLevel();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height;
            (SerializedProperty? key, SerializedProperty? value) = GetChildProperty(property);
            if (key == null || value == null)
                height = base.GetPropertyHeight(property, label);
            else
                height = Max(EditorGUI.GetPropertyHeight(key), EditorGUI.GetPropertyHeight(value));

            if (EditorGUIUtility.wideMode || !LabelHasContent(label))
                return height;
            else
                return height + EditorGUIUtility.singleLineHeight + 4;
        }

        public static (SerializedProperty? key, SerializedProperty? value) GetChildProperty(SerializedProperty property) => (property.FindPropertyRelative(SerializableKeyValuePair.nameOfInternalKey), property.FindPropertyRelative(SerializableKeyValuePair.nameOfInternalValue));
    }
}