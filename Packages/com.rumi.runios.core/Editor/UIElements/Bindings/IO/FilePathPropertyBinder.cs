#nullable enable
using RuniOS.Editor.Drawers.IO;
using RuniOS.IO;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings.IO
{
    [CustomPropertyBinder(typeof(FilePath))]
    public class FilePathPropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType) => new FilePath(FilePathPropertyDrawer.GetChildProperty(property).stringValue);
        
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value)
        {
            if (value is FilePath filePath)
                FilePathPropertyDrawer.GetChildProperty(property).stringValue = filePath.value;
        }
    }
}