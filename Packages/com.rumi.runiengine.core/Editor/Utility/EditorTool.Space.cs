#nullable enable
using UnityEngine;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static void Space() => GUILayout.Space(10);
        public static void Space(int width) => GUILayout.Space(width);

        public static void TabSpace(int tab)
        {
            if (tab > 0)
                GUILayout.Space(30 * tab);
        }
    }
}
