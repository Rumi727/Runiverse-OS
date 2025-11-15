#nullable enable
using RuniOS.Editor.IMGUI.Drawers.IO;
using RuniOS.IO;

namespace RuniOS.Editor.Serialization.Converters.IO
{
    [CustomPropertyConverter(typeof(FileExtension))]
    public class FileExtensionPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => new FileExtension(FileExtensionPropertyDrawer.GetChildProperty(property).stringValue);
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is FileExtension fileExtension)
                FileExtensionPropertyDrawer.GetChildProperty(property).stringValue = fileExtension.value;
        }
    }
}