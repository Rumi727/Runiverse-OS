#nullable enable
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class VersionRangeConverter : UxmlAttributeConverter<VersionRange>
    {
        public override VersionRange FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new VersionRange();
            
            return new VersionRange(value);
        }
        
        public override string ToString(VersionRange value) => value.ToString();
    }
}