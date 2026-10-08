#nullable enable
using RuniOS.IO;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.IO
{
    public sealed class RuniPathConverter : UxmlAttributeConverter<RuniPath>
    {
        public override RuniPath FromString(string value) => (RuniPath)value;
        public override string ToString(RuniPath value) => value.value;
    }
}