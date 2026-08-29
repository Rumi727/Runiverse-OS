#nullable enable
using RuniOS.Editor.Unity.Drawers.IO;
using RuniOS.IO;

namespace RuniOS.Editor.Unity.Serialization.Converters.IO
{
    [PropertyConverter(typeof(RuniPath))]
    public class RuniPathPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (RuniPath)RuniPathPropertyDrawer.GetChildProperty(property).stringValue;
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is RuniPath runiPath)
                RuniPathPropertyDrawer.GetChildProperty(property).stringValue = runiPath.value;
        }
    }
}