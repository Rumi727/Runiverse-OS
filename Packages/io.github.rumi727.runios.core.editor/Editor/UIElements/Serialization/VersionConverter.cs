#nullable enable
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class VersionConverter : UxmlAttributeConverter<Version>
    {
        public override Version FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Version.all;
            
            return new Version(value);
        }
        
        public override string ToString(Version value) => value.ToString();
    }
}