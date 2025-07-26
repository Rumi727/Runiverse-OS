#nullable enable
using RuniEngine.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(RectCorner))]
    public class RectCornerPropertyDrawer : AnimFolderPropertyDrawer { }
}
