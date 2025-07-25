#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static void DrawHLine(int thickness = 1, int padding = 10) => DrawHLine(new Color(0.4980392f, 0.4980392f, 0.4980392f), thickness, padding);

        public static void DrawHLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding - 2));
            r.height = thickness;
            r.y += (padding / 2f) - 2;
            r.x -= 18;
            r.width += 22;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawVLine(int thickness = 1, int padding = 10) => DrawVLine(new Color(0.4980392f, 0.4980392f, 0.4980392f), thickness, padding);

        public static void DrawVLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(padding - 2));
            r.width = thickness;
            r.x += (padding / 2f) - 2;
            r.y -= 18;
            r.height += 22;
            EditorGUI.DrawRect(r, color);
        }
    }
}
