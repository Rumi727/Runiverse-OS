#nullable enable
using RuniOS.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniOS.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(UnlimitedDateTime))]
    public class UnlimitedDateTimePropertyDrawer : AnimFolderPropertyDrawer { }
}
