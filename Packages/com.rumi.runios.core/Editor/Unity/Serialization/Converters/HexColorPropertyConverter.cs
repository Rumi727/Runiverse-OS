#nullable enable
using RuniOS.Editor.Unity.Drawers;

namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [PropertyConverter(typeof(HexColor))]
    public class HexColorPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => new HexColor(HexColorPropertyDrawer.GetChildProperty(property).stringValue);
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is HexColor hexColor)
                HexColorPropertyDrawer.GetChildProperty(property).stringValue = hexColor.value;
        }
    }
}