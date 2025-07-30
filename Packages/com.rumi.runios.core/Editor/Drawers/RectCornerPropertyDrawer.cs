#nullable enable
using RuniOS.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniOS.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(RectCorner))]
    public class RectCornerPropertyDrawer : AnimFolderPropertyDrawer { }
}
