#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ISerializableKeyValuePair<,>), true)]
    public class SerializableKeyValuePairPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            (SerializedProperty? key, SerializedProperty? value) = GetChildProperty(property);
            if (key == null)
            {
                GUI.Label(position, GetTextOrKey("serializable_key_value_pair_property_drawer.not_found.key"));
                return;
            }
            else if (value == null)
            {
                GUI.Label(position, GetTextOrKey("serializable_key_value_pair_property_drawer.not_found.value"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3); // 2로 하면 크기 절반 줄어듬
            
            BeginIndentLevel(0);
            float fieldWidth = (position.width - 15) / 2f;

            position.y += 1;

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
                position.height = EditorGUI.GetPropertyHeight(key, valueLabelContent);

                BeginLabelWidth(valueLabel);
                EditorGUI.PropertyField(position, value, valueLabelContent);
                EndLabelWidth();
            }

            EndIndentLevel();
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            (SerializedProperty? key, SerializedProperty? value) = GetChildProperty(property);
            if (key == null || value == null)
                return base.GetPropertyHeight(property, label);

            return EditorGUI.GetPropertyHeight(key).Max(EditorGUI.GetPropertyHeight(value));
        }

        public static (SerializedProperty? key, SerializedProperty? value) GetChildProperty(SerializedProperty property) => (property.FindPropertyRelative(SerializableKeyValuePair.nameofKey), property.FindPropertyRelative(SerializableKeyValuePair.nameofValue));
    }
}
