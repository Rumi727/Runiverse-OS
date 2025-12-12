#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static float GetMultiColumnsFieldHeight(GUIContent? label, int rows = 1)
        {
            float height = (EditorGUIUtility.singleLineHeight * rows) + ((rows - 1) * EditorGUIBridge.kControlVerticalSpacingLegacy);
            bool hasLabel = LabelHasContent(label);
            if (hasLabel && !EditorGUIUtility.wideMode)
                height += EditorGUIUtility.singleLineHeight + EditorGUIBridge.kControlVerticalSpacingLegacy;
            
            return height;
        }

        public static Rect GetMultiColumnsControlRect(GUIContent label, int rows = 1) => EditorGUILayout.GetControlRect(LabelHasContent(label), GetMultiColumnsFieldHeight(label, rows));

        public static Rect DrawMultiColumnsFieldPrefixLabel(Rect position, GUIContent label, int columns)
        {
            int controlId = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            return EditorGUIBridge.MultiFieldPrefixLabel(position, controlId, label, columns);
        }

        public static float GetMultiRowsFieldHeight(GUIContent label, int rows)
        {
            float height = (EditorGUIUtility.singleLineHeight * rows) + ((rows - 1) * EditorGUIBridge.kControlVerticalSpacingLegacy);
            bool hasLabel = LabelHasContent(label); 
            if (hasLabel)
                height += EditorGUIUtility.singleLineHeight + EditorGUIBridge.kControlVerticalSpacingLegacy;
            
            return height;
        }

        public static Rect GetMultiRowsControlRect(GUIContent label, int rows) => EditorGUILayout.GetControlRect(LabelHasContent(label), GetMultiRowsFieldHeight(label, rows));

        public static Rect DrawMultiRowsPrefixLabel(Rect position, GUIContent label)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            if (EditorGUIUtility.wideMode)
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIBridge.kControlVerticalSpacingLegacy;

            return position;
        }

        public static Rect DrawMultiRowsLabel(Rect position, bool drawOutside, params GUIContent[] contents)
        {
            float labelXSize = GetLabelXSize(contents);
            Rect labelPosition = position;
            if (drawOutside)
                labelPosition.x -= labelXSize + 2;
            
            labelPosition.width = labelXSize;

            BeginAlignment(TextAnchor.MiddleRight, GUI.skin.label);

            for (int i = 0; i < contents.Length; i++)
            {
                GUI.Label(labelPosition, contents[i]);
                labelPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIBridge.kControlVerticalSpacingLegacy;
            }
            
            EndAlignment(GUI.skin.label);
            
            if (!drawOutside)
                position.xMin += labelXSize + 2;
            
            return position;
        }
    }
}