#nullable enable
using RuniOS.IO;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class FileExtensionConverter : UxmlAttributeConverter<FileExtension>
    {
        public override FileExtension FromString(string value) => value;
        public override string ToString(FileExtension value) => value;
    }
}
