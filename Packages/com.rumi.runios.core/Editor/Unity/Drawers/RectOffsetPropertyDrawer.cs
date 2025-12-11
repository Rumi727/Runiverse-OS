#nullable enable
using RuniOS.Editor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Unity.Drawers
{
    [CustomPropertyDrawer(typeof(RectOffset))]
    public class RectOffsetPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new RectOffsetField().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        static readonly GUIContent[] labels = new GUIContent[] { new GUIContent("L"), new GUIContent("R"), new GUIContent("T"), new GUIContent("B") };
        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            property = property.Copy();
            property.Next(true);
            
            EditorGUI.MultiPropertyField(position, labels, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => GetMultiColumnsFieldHeight(label);
    }
}