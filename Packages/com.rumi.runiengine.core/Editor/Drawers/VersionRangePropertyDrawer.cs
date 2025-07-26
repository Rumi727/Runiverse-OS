#nullable enable
using RuniEngine.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(VersionRange))]
    public class VersionRangePropertyDrawer : AnimFolderPropertyDrawer { }
}
