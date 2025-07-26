#nullable enable
using RuniEngine.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(UnlimitedDateTime))]
    public class UnlimitedDateTimePropertyDrawer : AnimFolderPropertyDrawer { }
}
