#nullable enable
using RuniOS.Editor.Drawers.IO;
using RuniOS.IO;
using System;
using UnityEditor;

namespace RuniOS.Editor.Serialization.Converters.IO
{
    [CustomPropertyConverter(typeof(FilePath))]
    public class FilePathPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => new FilePath(FilePathPropertyDrawer.GetChildProperty(property).stringValue);
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is FilePath filePath)
                FilePathPropertyDrawer.GetChildProperty(property).stringValue = filePath.value;
        }
    }
}