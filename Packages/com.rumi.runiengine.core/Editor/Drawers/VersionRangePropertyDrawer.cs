#nullable enable
using RuniOS.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniOS.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(VersionRange))]
    public class VersionRangePropertyDrawer : AnimFolderPropertyDrawer { }
}
