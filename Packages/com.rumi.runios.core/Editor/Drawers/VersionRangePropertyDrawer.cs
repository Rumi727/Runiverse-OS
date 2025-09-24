#nullable enable
using RuniOS.Editor.Drawers.Attributes;
using UnityEditor;

namespace RuniOS.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(VersionRange))]
    public class VersionRangePropertyDrawer : AnimFolderPropertyDrawer
    {
        public static (SerializedProperty min, SerializedProperty max) GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            
            property.Next(true);
            SerializedProperty major = property.Copy();
            
            property.Next(false);
            SerializedProperty minor = property.Copy();

            return (major, minor);
        }
    }
}
