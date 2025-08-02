#nullable enable
using RuniOS.Editor.Drawers.IO;
using RuniOS.Editor.Drawers.Resource;
using RuniOS.Resource;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(Identifier))]
    public class IdentifierPropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType)
        {
            (SerializedProperty nameSpace, SerializedProperty path) = IdentifierPropertyDrawer.GetChildProperty(property);
            path = FilePathPropertyDrawer.GetChildProperty(path);
            
            return new Identifier(nameSpace.stringValue, path.stringValue);
        }
        
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value)
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