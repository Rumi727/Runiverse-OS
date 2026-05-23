#nullable enable
namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static RectOffset RectOffsetFieldLayout(RectOffset value) => RectOffsetFieldLayout(GUIContent.none, value);
        public static RectOffset RectOffsetFieldLayout(string label, RectOffset value) => RectOffsetFieldLayout(new GUIContent(label), value);
        public static RectOffset RectOffsetFieldLayout(GUIContent label, RectOffset value) => RectOffsetField(GetMultiColumnsControlRect(label), label, value);

        public static RectOffset RectOffsetField(Rect position, RectOffset value) => DoRectOffsetField(position, value);
        public static RectOffset RectOffsetField(Rect position, string label, RectOffset value) => RectOffsetField(position, new GUIContent(label), value);
        public static RectOffset RectOffsetField(Rect position, GUIContent label, RectOffset value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 4);
            return DoRectOffsetField(position, value);
        }

        static readonly GUIContent[] rectOffsetLabels = [new GUIContent("L"), new GUIContent("R"), new GUIContent("T"), new GUIContent("B")];
        static readonly float[] rectOffsetValues = new float[4];
        static RectOffset DoRectOffsetField(Rect position, RectOffset value)
        {
            rectOffsetValues[0] = value.left;
            rectOffsetValues[1] = value.right;
            rectOffsetValues[2] = value.top;
            rectOffsetValues[3] = value.bottom;

            EditorGUI.MultiFloatField(position, rectOffsetLabels, rectOffsetValues);

            value.left = rectOffsetValues[0];
            value.right = rectOffsetValues[1];
            value.top = rectOffsetValues[2];
            value.bottom = rectOffsetValues[3];

            return value;
        }
    }
}