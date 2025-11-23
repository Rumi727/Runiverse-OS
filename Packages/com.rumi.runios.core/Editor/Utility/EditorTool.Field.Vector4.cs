#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Vector4 Vector4FieldLayout(Vector4 value) => Vector4Field(GetMultiControlRect(), value);
        public static Vector4 Vector4FieldLayout(string label, Vector4 value) => Vector4Field(GetMultiControlRect(), label, value);
        public static Vector4 Vector4FieldLayout(GUIContent label, Vector4 value) => Vector4Field(GetMultiControlRect(), label, value);

        public static Vector4 Vector4Field(Rect position, Vector4 value) => DoVector4Field(position, value);
        public static Vector4 Vector4Field(Rect position, string label, Vector4 value) => Vector4Field(position, new GUIContent(label), value);
        public static Vector4 Vector4Field(Rect position, GUIContent label, Vector4 value)
        {
            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 4); // 2로 하면 크기 절반 줄어듬

            BeginIndentLevel(0);
            value = DoVector4Field(position, value);
            EndIndentLevel();
            return value;
        }

        static readonly GUIContent[] vector4Labels = new GUIContent[]
        {
            new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W")
        };
        static readonly float[] vector4Values = new float[4];
        static Vector4 DoVector4Field(Rect position, Vector4 value)
        {
            vector4Values[0] = value.x;
            vector4Values[1] = value.y;
            vector4Values[2] = value.z;
            vector4Values[3] = value.w;

            EditorGUI.MultiFloatField(position, vector4Labels, vector4Values);

            value.x = vector4Values[0];
            value.y = vector4Values[1];
            value.z = vector4Values[2];
            value.w = vector4Values[3];

            return value;
        }
    }
}