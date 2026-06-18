#nullable enable
using RuniOS.Editor.Unity.Drawers.IO;
using RuniOS.IO;

namespace RuniOS.Editor.Unity.Serialization.Converters.IO
{
    [CustomPropertyConverter(typeof(PhysicalPath))]
    public class PhysicalPathPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (PhysicalPath)PhysicalPathPropertyDrawer.GetChildProperty(property).stringValue;
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is PhysicalPath path)
                PhysicalPathPropertyDrawer.GetChildProperty(property).stringValue = path.value;
        }
    }
}