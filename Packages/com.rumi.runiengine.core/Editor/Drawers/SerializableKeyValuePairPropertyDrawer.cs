#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ISerializableKeyValuePair<,>), true)]
    public class SerializableKeyValuePairPropertyDrawer : PropertyDrawer
    {
        SerializedProperty? key;
        SerializedProperty? value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitProperty(property);
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

            {
                position.width = fieldWidth;

                string keyLabel = GetTextOrKey("gui.key");

                BeginLabelWidth(keyLabel);
                EditorGUI.PropertyField(position, key, new GUIContent(keyLabel));
                EndLabelWidth();

                position.x += position.width + 15;
            }

            {
                position.width = fieldWidth.Ceil();

                string valueLabel = GetTextOrKey("gui.value");

                BeginLabelWidth(valueLabel);
                EditorGUI.PropertyField(position, value, new GUIContent(valueLabel));
                EndLabelWidth();
            }

            EndIndentLevel();
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitProperty(property);
            if (key == null || value == null)
                return base.GetPropertyHeight(property, label);

            return EditorGUI.GetPropertyHeight(key).Max(EditorGUI.GetPropertyHeight(value));
        }

        public void InitProperty(SerializedProperty property)
        {
            key ??= property.FindPropertyRelative("key");
            value ??= property.FindPropertyRelative("value");
        }
    }
}
