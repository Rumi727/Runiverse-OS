#nullable enable
using RuniOS.Resource;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Resource
{
    public sealed class RegistryTypeConverter : UxmlAttributeConverter<RegistryType>
    {
        public override RegistryType FromString(string value) => value;
        public override string ToString(RegistryType value) => value;
    }
}