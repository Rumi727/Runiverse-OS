#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static void DrawHLine(int thickness = 1, int padding = 10) => DrawHLine(new Color(0.4980392f, 0.4980392f, 0.4980392f), thickness, padding);

        public static void DrawHLine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding - 2));
            r.height = thickness;
            r.y += (padding / 2f) - 2;
            r.x -= 18;
            r.width += 22;
            EditorGUI.DrawRect(r, color);
        }
    }
}
