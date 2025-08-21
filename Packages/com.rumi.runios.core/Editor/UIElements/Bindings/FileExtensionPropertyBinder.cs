#nullable enable
using RuniOS.Editor.Drawers.IO;
using RuniOS.IO;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(FileExtension))]
    public class FileExtensionPropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType) => new FileExtension(FileExtensionPropertyDrawer.GetChildProperty(property).stringValue);
        
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value)
        {
            if (value is FileExtension fileExtension)
                FileExtensionPropertyDrawer.GetChildProperty(property).stringValue = fileExtension.value;
        }
    }
}