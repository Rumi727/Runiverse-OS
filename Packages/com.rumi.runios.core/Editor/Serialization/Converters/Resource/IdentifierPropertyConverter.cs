#nullable enable
using RuniOS.Editor.IMGUI.Drawers.IO;
using RuniOS.Editor.IMGUI.Drawers.Resource;
using RuniOS.Resource;

namespace RuniOS.Editor.Serialization.Converters.Resource
{
    [CustomPropertyConverter(typeof(Identifier))]
    public class IdentifierPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            (SerializedProperty nameSpace, SerializedProperty path) = IdentifierPropertyDrawer.GetChildProperty(property);
            path = FilePathPropertyDrawer.GetChildProperty(path);
            
            return new Identifier(nameSpace.stringValue, path.stringValue);
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is Identifier identifier)
            {
                (SerializedProperty nameSpace, SerializedProperty path) = IdentifierPropertyDrawer.GetChildProperty(property);
                path = FilePathPropertyDrawer.GetChildProperty(path);
                
                nameSpace.stringValue = identifier.nameSpace;
                path.stringValue = identifier.path;
            }
        }
    }
}