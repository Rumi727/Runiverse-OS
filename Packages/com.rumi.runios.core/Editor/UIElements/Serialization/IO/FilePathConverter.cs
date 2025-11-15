#nullable enable
using RuniOS.IO;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.IO;

public sealed class FilePathConverter : UxmlAttributeConverter<FilePath>
{
    public override FilePath FromString(string value) => value;
    public override string ToString(FilePath value) => value;
}