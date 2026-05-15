#nullable enable
using RuniOS.Editor.Unity.Drawers.IO;
using RuniOS.Editor.Unity.Drawers.Resource;
using RuniOS.Resource;

namespace RuniOS.Editor.Unity.Serialization.Converters.Resource
{
    [CustomPropertyConverter(typeof(Identifier))]
    public class IdentifierPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            (SerializedProperty nameSpace, SerializedProperty path) = IdentifierPropertyDrawer.GetChildProperty(property);
            path = RuniPathPropertyDrawer.GetChildProperty(path);
            
            return new Identifier(nameSpace.stringValue, path.stringValue);
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is Identifier identifier)
            {
                (SerializedProperty nameSpace, SerializedProperty path) = IdentifierPropertyDrawer.GetChildProperty(property);
                path = RuniPathPropertyDrawer.GetChildProperty(path);
                
                nameSpace.stringValue = identifier.nameSpace;
                path.stringValue = identifier.path.value;
            }
        }
    }
}