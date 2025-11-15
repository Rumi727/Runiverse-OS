#nullable enable
namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static void DrawHLine(int thickness = 1, int padding = 10) => DrawHLine(EditorGUIUtility.isProSkin ? new Color32(26, 26, 26, 255) : new Color32(127, 127, 127, 255), thickness, padding);

        public static void DrawHLine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding - 2));
            r.height = thickness;
            r.y += (padding / 2f) - 2;
            r.x -= 3;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}