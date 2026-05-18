#nullable enable
using RuniOS.IO;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.IO
{
    public sealed class PhysicalPathConverter : UxmlAttributeConverter<PhysicalPath>
    {
        public override PhysicalPath FromString(string value) => (PhysicalPath)value;
        public override string ToString(PhysicalPath value) => value.value;
    }
}