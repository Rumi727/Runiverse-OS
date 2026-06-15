#nullable enable

using RuniOS.Collections.Generic;
using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor.Unity.Drawers
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

            string keyLabel = GetTextOrKey("gui.key");
            GUIContent keyLabelContent = new GUIContent(keyLabel);
            float keyHeight = EditorGUI.GetPropertyHeight(key, keyLabelContent);

            string valueLabel = GetTextOrKey("gui.value");
            GUIContent valueLabelContent = new GUIContent(valueLabel);
            float valueHeight = EditorGUI.GetPropertyHeight(value, valueLabelContent);

            bool isField = Max(keyHeight, valueHeight) <= EditorGUIUtility.singleLineHeight;

            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 3); // 2로 하면 크기 절반 줄어듬
            
            BeginIndentLevel(0);
            float fieldWidth = isField ? (position.width - 15) / 2f : position.width;

            {
                position.width = fieldWidth;
                position.height = keyHeight;

                if (isField)
                    BeginLabelWidth(keyLabel);

                EditorGUI.PropertyField(position, key, keyLabelContent);

                if (isField)
                    EndLabelWidth();

                if (isField)
                    position.x += position.width + 15;
            }

            if (!isField)
                position.y += position.height;

            {
                position.width = fieldWidth.Ceil();
                position.height = valueHeight;

                if (isField)
                    BeginLabelWidth(valueLabel);

                EditorGUI.PropertyField(position, value, valueLabelContent);

                if (isField)
                    EndLabelWidth();
            }

            EndIndentLevel();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float wideHeight = 0;
            if (!EditorGUIUtility.wideMode && LabelHasContent(label))
                wideHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            (SerializedProperty? key, SerializedProperty? value) = GetChildProperty(property);
            if (key == null || value == null)
                return EditorGUIUtility.singleLineHeight + wideHeight;

            string keyLabel = GetTextOrKey("gui.key");
            GUIContent keyLabelContent = new GUIContent(keyLabel);
            float keyHeight = EditorGUI.GetPropertyHeight(key, keyLabelContent);

            string valueLabel = GetTextOrKey("gui.value");
            GUIContent valueLabelContent = new GUIContent(valueLabel);
            float valueHeight = EditorGUI.GetPropertyHeight(value, valueLabelContent);

            float fieldHeight = Max(keyHeight, valueHeight);
            bool isField = Max(keyHeight, valueHeight) <= EditorGUIUtility.singleLineHeight;

            if (!isField)
                fieldHeight += EditorGUIUtility.singleLineHeight;

            return fieldHeight + wideHeight;
        }

        public static (SerializedProperty? key, SerializedProperty? value) GetChildProperty(SerializedProperty property) => (property.FindPropertyRelative(SerializableKeyValuePair.nameOfInternalKey), property.FindPropertyRelative(SerializableKeyValuePair.nameOfInternalValue));
    }
}